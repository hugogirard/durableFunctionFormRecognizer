param location string

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

module functionSeeder 'modules/functions/seeder.bicep' = {
  name: 'functionSeeder'
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
