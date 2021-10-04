const fs = require('fs').promises;
const ContainerClient = require('../Factory/containerClientFactory');
const ONE_MEGABYTE = 1024 * 1024;
const uploadOptions = { bufferSize: 4 * ONE_MEGABYTE, maxBuffers: 20 };

module.exports = async function (context) {

    const result = {
        filename: context.bindings.input.filename,        
        isSuccess: false,
        error: null
    };

    try {
        
        const containerClient = ContainerClient(context.bindings.input.containerName);
        const blockBlobClient = containerClient.getBlockBlobClient(result.filename);

        const filePath = `${__dirname}/template/Invoice_Template.pdf`;
        const data = await fs.readFile(filePath,'binary');

        const uploadBlobResponse = await blockBlobClient.uploadStream(data);

        const tags = {
            'status': 'notProcessed'
        };

        const tagsResponse = await blockBlobClient.setTags(tags);

        result.isSuccess = true;

    } catch (error) {
        context.log.error(error);
        context.log.error(`Cannot write file`);
        result.error = error;      
    }

    return result;
};