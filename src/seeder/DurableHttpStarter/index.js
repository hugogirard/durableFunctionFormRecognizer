const df = require("durable-functions");
const { BlobServiceClient } = require('@azure/storage-blob');

module.exports = async function (context, req) {

    try {
        const cnxString = process.env['DocumentStorage'];
    
        context.log.info(`Body payload parameters: ${context.rawBody}`);

        // Get payload HTTP Post
        const body = req.body;        

        // Validate parameters
        if (!body.year || !body.month || !body.day) {
            context.log.error(`Invalid body parameters`);
            context.log.error(`Body payload parameters: ${context.rawBody}`);
            throw new Exception('Invalid Body Parameter');
        }
                
        const blobServiceClient = BlobServiceClient.fromConnectionString(cnxString);
    
        const containerName = `${body.year}-${body.month}-${body.day}`;
        context.log.info(`Create container name: ${containerName}`);
    
        const containerClient = blobServiceClient.getContainerClient(containerName);
        
        const containerResponse = await containerClient.createIfNotExists();
        
        if (!containerResponse.succeeded && containerResponse.errorCode != 'ContainerAlreadyExists'){
            context.log.error(`Cannot create container ${containerName}`);
            throw new Exception('Cannot create container');
        }
        
        req.body.containerName = containerName; 

        const client = df.getClient(context);
        const instanceId = await client.startNew('SeederOrchestrator', undefined, req.body);
    
        context.log(`Started orchestration with ID = '${instanceId}'.`);
    
        return client.createCheckStatusResponse(context.bindingData.req, instanceId);        
    } catch (error) {
        context.log.error(error);
        throw new Expection(error);
    }


};