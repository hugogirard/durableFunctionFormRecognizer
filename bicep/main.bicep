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
    strAccountDocumentName: storage.outputs.strDocumentName
    strAccountDocumentId: storage.outputs.strDocumentId
    strAccountDocumentApiVersion: storage.outputs.strDocumentApiVersion
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

output seederFunctionName string = functionSeeder.outputs.functionSeederName
