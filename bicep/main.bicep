param location string
param addressSpaceSubnet string
param addressSpaceVnet string
@secure()
param adminUsername string

@secure()
param adminPassword string

var suffix = uniqueString(resourceGroup().id)

module cognitives 'modules/cognitives/form.bicep' = {
  name: 'cognitives'
  params: {
    location: location
    suffix: suffix
  }
}

module insight 'modules/insights/insights.bicep' = {
  name: 'insight'
  params: {
    location: location
    suffix: suffix    
  }
}

module storage 'modules/storage/storage.bicep' = {
  name: 'storage'
  params: {
    location: location
    suffix: suffix
  }
}

module vnet 'modules/network/vnet.bicep' = {
  name: 'vnet'
  params: {
    location: location
    addressSpaceVnet: addressSpaceVnet
    addressSpaceSubnet: addressSpaceSubnet
  }
}

module compute 'modules/compute/windows.bicep' = {
  name: 'compute'
  params: {
    location: location
    suffix: suffix
    subnetId: vnet.outputs.subnetId
    adminUsername: adminUsername
    adminPassword: adminPassword
  }
}

module functionProcessor 'modules/functions/processor.bicep' = {
  name: 'functionProcessor'
  params: {
    location: location
    suffix: suffix
    appInsightCnxString: insight.outputs.appInsightCnxString
    appInsightKey: insight.outputs.appInsightKey    
    strAccountApiVersion: storage.outputs.strFunctionApiVersion
    strAccountId: storage.outputs.strFunctionId
    strAccountName: storage.outputs.strFunctionName    
  }
}

output vmName string = compute.outputs.vmName
