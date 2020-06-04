Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$env:HOME=""
$az = "az"

function Assert-ResourceGroup($resourceGroup, $subscriptionId, $location)
{
    $exists = (&$az group exists --name "$resourceGroup" --subscription "$subscriptionId")
    if (-not ([System.Convert]::ToBoolean($exists)))
    {
        &$az group create --name "$resourceGroup" --subscription "$subscriptionId" --location "$location"
            |ConvertFrom-Json   
	}
}


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
          ],
        "httpsOnly": true
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

function Assert-Arm($resourceGroup, $subscriptionId, $templateFile)
{
    &$az deployment group create --resource-group "$resourceGroup" --subscription "$subscriptionId" --template-file "$templateFile" --mode Incremental
        |ConvertFrom-Json
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
    # Not sure what is causing color corruption, but this clears it up
    [Console]::ResetColor()
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
    Assert-Arm $appServiceResourceGroup $($tmp.FullName)
    Remove-Item $tmp
}
