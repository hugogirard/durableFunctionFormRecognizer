param location string
param suffix string

var frmName = 'frm-${suffix}'

resource frmRecognizer 'Microsoft.CognitiveServices/accounts@2021-04-30' = {
  name: frmName
  location: location
  sku: {
    name: 'S0'
  }
  kind: 'FormRecognizer'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    customSubDomainName: frmName
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
      virtualNetworkRules: [
        
      ]
    }
  }
}

//output frmEndpoint string = frmRecognizer.
