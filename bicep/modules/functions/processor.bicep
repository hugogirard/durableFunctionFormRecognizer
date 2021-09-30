param location string
param suffix string
param appInsightKey string
param appInsightCnxString string
param strAccountName string
param strAccountId string
param strAccountApiVersion string

var appServiceName = 'plan-processor-${suffix}'
var functionAppName = 'fnc-processor-${suffix}'

resource serverFarm 'Microsoft.Web/serverfarms@2020-06-01' = {
  name: appServiceName
  location: location
  sku: {
    name: 'EP1'
    tier: 'ElasticPremium'
  }
  kind: 'elastic'
  properties: {
  }
}

resource function 'Microsoft.Web/sites@2020-06-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: serverFarm.id
    siteConfig: {
      appSettings: [
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsightKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: 'InstrumentationKey=${appInsightCnxString}'
        }
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${strAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(strAccountId, strAccountApiVersion).keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${strAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(strAccountId, strAccountApiVersion).keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: 'processorapp092'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
          name: 'WEBSITE_NODE_DEFAULT_VERSION'
          value: '~12'
        }
      ]
    }
  }
}
