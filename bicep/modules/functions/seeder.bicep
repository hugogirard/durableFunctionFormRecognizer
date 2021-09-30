param location string
param suffix string
param appInsightKey string
param appInsightCnxString string
param strAccountName string
param strAccountId string
param strAccountApiVersion string

var appServiceName = 'plan-seeder-${suffix}'
var functionAppName = 'fnc-seeder-${suffix}'

resource appService 'Microsoft.Web/serverFarms@2020-06-01' = {
  name: appServiceName
  location: location
  kind: 'functionapp'
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
    size: 'Y1'
    family: 'Y'
    capacity: 0
  }
  properties: {
  }
}

resource functionApp 'Microsoft.Web/sites@2020-06-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  properties: {
    enabled: true
    hostNameSslStates: [
      {
        name: '${functionAppName}.azurewebsites.net'
        sslState: 'Disabled'
        hostType: 'Standard'
      }
      {
        name: '${functionAppName}.scm.azurewebsites.net'
        sslState: 'Disabled'
        hostType: 'Standard'
      }
    ]
    serverFarmId: appService.id
    reserved: false
    isXenon: false
    hyperV: false
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${strAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(strAccountId, strAccountApiVersion).keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${strAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(strAccountId, strAccountApiVersion).keys[0].value}'
        }
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appInsightKey
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: 'InstrumentationKey=${appInsightCnxString}'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: 'seederapp092'
        }                
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'node'
        }
        {
          name: 'WEBSITE_NODE_DEFAULT_VERSION'
          value: '~14'
        }               
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~3'
        }
      ]
    }
    scmSiteAlsoStopped: false
    clientAffinityEnabled: false
    clientCertEnabled: false
    hostNamesDisabled: false
    dailyMemoryTimeQuota: 0
    httpsOnly: true
    redundancyMode: 'None'
  }  
}
