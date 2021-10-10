param location string
param addressSpaceVnet string
param addressSpaceSubnet string

resource nsg 'Microsoft.Network/networkSecurityGroups@2020-06-01' =  {
  name: 'nsg-seeder'
  location: location
  properties: {
    securityRules: [

    ]
  }
}

resource vnet 'Microsoft.Network/virtualNetworks@2021-02-01'= {
  name: 'vnet-seeder'
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [
        addressSpaceVnet
      ]
    }
    subnets: [
      {
        name: 'snet-seeder'
        properties: {
          addressPrefix: addressSpaceSubnet
          networkSecurityGroup: {
            id: nsg.id
          }
        }
      }
    ]
  }
}

output subnetId string = vnet.properties.subnets[0].id
