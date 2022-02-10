const axios = require('axios');

exports.handler = async function(event) {
    console.log(JSON.stringify(event));

    if (event.Records != null){
        event = JSON.parse(event.Records[0].Sns.Message);
        if (event.RequestType === "Delete") {
            console.log("got a delete request")
            await postBackSuccess(event);
            return;
        }
        if (event.RequestType === "Update")
        {
            console.log("got an update request")
            let oldAccessKey = event.OldResourceProperties.CloudGuardApiKeyId;
            let newAccessKey = event.ResourceProperties.CloudGuardApiKeyId;

            let oldDeleteInnerResources = event.OldResourceProperties.DeleteInnerResources;
            let newDeleteInnerResources = event.ResourceProperties.DeleteInnerResources;

            if (oldAccessKey === newAccessKey && oldDeleteInnerResources === newDeleteInnerResources || newDeleteInnerResources === "true" && oldAccessKey !== newAccessKey)
            {
                await postBackSuccess(event);
            }
        }
    }
}

async function postBackSuccess(event){
    let responseUrl = event.ResponseURL;
    let response = {
        "Status" : "SUCCESS",
        "PhysicalResourceId" : "TestResource1",
        "StackId" : event.StackId,
        "RequestId" : event.RequestId,
        "LogicalResourceId" : event.LogicalResourceId,
    }

    let res = await axios.put(responseUrl, response)
    console.log(res)
}