// Create static client of storage
const { BlobServiceClient } = require('@azure/storage-blob');
const strCnxString = process.env.DocumentStorage || "";
const blobServiceClient = BlobServiceClient.fromConnectionString(strCnxString);
let containerClients = [];
const instanceService = (containerName) => {
    let instance = containerClients.find(e => e.name === containerName);
    if (instance) {
        instance = instance.client;
    } else {        
        instance = blobServiceClient.getContainerClient(containerName);
        containerClients.push({
            name: containerName,
            client: instance
        });        
    }        
    return instance;
};
module.exports = instanceService;
