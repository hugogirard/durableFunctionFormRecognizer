const fs = require('fs').promises;

module.exports = async function (context) {

    const result = {
        filename: context.bindings.input.filename,
        isSuccess: false,
        error: null
    };

    try {
        
        const filePath = `${__dirname}/template/Invoice_Template.pdf`;
        const data = await fs.readFile(filePath,'binary');

        context.bindings.outputBlob = data;
        result.isSuccess = true;

    } catch (error) {
        context.log.error(error);
        context.log.error(`Cannot write file`);
        result.error = error;      
    }

    return result;
};