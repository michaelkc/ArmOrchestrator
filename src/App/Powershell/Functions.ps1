Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$env:HOME=""
$az = "az"

function Assert-AppServiceManagedCertificate($appServiceResourceGroup, $appServiceName, $cname)
{
    &$az webapp config ssl create --resource-group "$appServiceResourceGroup" --name "$appServiceName" --hostname "$cname"
        |ConvertFrom-Json   
}

Function Get-CNameToCertificateBindingArmScript($appServiceName,$location,$cname,$thumbprint)
{
    $arm = 
@'
{
    "$schema": "https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#",
    "contentVersion": "1.0.0.0",
    "parameters": {},
    "variables": {},
    "resources": [
      {
        "type": "Microsoft.Web/sites",
        "kind": "app",
        "name": "##APPSERVICENAME##",
        "apiVersion": "2016-08-01",
        "location": "##LOCATION##",
        "properties": {
          "hostNameSslStates": [
            {
              "name":"##CNAME##",
              "sslState":"SniEnabled",
              "ipBasedSslResult":null,
              "virtualIP":null,
              "thumbprint":"##THUMBPRINT##",
              "toUpdate":true,
              "toUpdateIpBasedSsl":null,
              "iPBasedSslState":"NotConfigured",
              "hostType":"Standard"
            }
          ]
        },
        "dependsOn": []
      }
    ]
  }  
'@
  $arm = $arm -replace "##THUMBPRINT##",$thumbprint
  $arm = $arm -replace "##CNAME##",$cname
  $arm = $arm -replace "##APPSERVICENAME##",$appServiceName
  $arm = $arm -replace "##LOCATION##",$location
  
  $arm
}
function Assert-TlsCnameBinding($subscriptionId, $appServiceName, $cname)
{
    $appService = &$az webapp list --subscription "$subscriptionId"
        | ConvertFrom-Json
        | Where-Object {$_.name -eq $appServiceName}
        | Select-Object -First 1
    $appServiceResourceGroup = $appService.resourceGroup
    $location = $appService.location

    # Will not recreate the cert if it is already created
    $certificate = (Assert-AppServiceManagedCertificate $appServiceResourceGroup $appServiceName $cname)
    $thumbprint = $certificate.thumbprint
    # Due to 
    # https://github.com/Azure/azure-cli/issues/9972
    # and the lack of documentation/other bugs around poking the hostNameSslStates array via update
    # the only remaining way to bind the new certificate to the host is via Arm 
    # Otherwise it would be something like
    # &$az webapp config ssl bind --resource-group "$appServiceResourceGroup" --name "$appServiceName" --certificate-thumbprint "$thumbprint" --ssl-type SNI --output JSON --debug
    $arm = Get-CNameToCertificateBindingArmScript $appServiceName $location $cname $thumbprint
    $tmp = New-TemporaryFile
    $arm | Out-File $tmp.FullName
    &$az deployment group create --resource-group "$appServiceResourceGroup" --template-file "$($tmp.FullName)" --mode Incremental
      |ConvertFrom-Json
    Remove-Item $tmp



}

Assert-TlsCnameBinding "8a3810d4-2f5b-4b66-90ca-9e96ac3e45be" "deleteme1-dev-wa" "armtemplater.segestest.dk"

