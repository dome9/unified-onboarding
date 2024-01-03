const fs = require('fs');
const {yamlParse, yamlDump} = require('yaml-cfn');
const {argv} = require('process');
const isDebug = process.env.isDebug || false;

const orchestrator = yamlParse(fs.readFileSync(__dirname + '/../replacements/orchestrator.yml', 'utf8'));
const orchestratorInvokeProperties = yamlParse(fs.readFileSync(__dirname + '/../replacements/orchestrator_invoke_properties.yml', 'utf8'));
const parameters = yamlParse(fs.readFileSync(__dirname + '/../replacements/parameters.yml', 'utf8'));
const readonlyPolicy = yamlParse(fs.readFileSync(__dirname + '/../replacements/readonly_policy.yml', 'utf8'));
const readonlyPolicyStatements = yamlParse(fs.readFileSync(__dirname + '/../replacements/readonly_policy_statements_cft.yml', 'utf8'));
const readwritePolicy = yamlParse(fs.readFileSync(__dirname + '/../replacements/readwrite_policy.yml', 'utf8'));
const stackModifyPolicyStatements = yamlParse(fs.readFileSync(__dirname + '/../replacements/stack_modify_policy_statements.yml', 'utf8'));
const metadata = yamlParse(fs.readFileSync(__dirname + '/../replacements/metadata.yml', 'utf8'));
const userBasedOrchestratorRolePolicies = yamlParse(fs.readFileSync(__dirname + '/../replacements/user_based_orchestrator_role_policy_statements.yml', 'utf8'));
const roleBasedOrchestratorRolePolicies = yamlParse(fs.readFileSync(__dirname + '/../replacements/role_based_orchestrator_role_policy_statements.yml', 'utf8'));
const sns = yamlParse(fs.readFileSync(__dirname + '/../replacements/sns.yml', 'utf8'));
const helperLambda = yamlParse(fs.readFileSync(__dirname + '/../replacements/orchestrator_helper.yml', 'utf8'));
const conditions = yamlParse(fs.readFileSync(__dirname + '/../replacements/conditions.yml', 'utf8'));
const ROLE_BASED_ONBOARDING = 'Role';
const USER_BASED_ONBOARDING = 'User';
const AWS_PARTITION = '${AWS::Partition}';

let bucketSuffix = '';
let outputDir = '';

argv.forEach((val, _) => {
    let split = val.split('=');
    if (split.length === 2) {
        if (split[0] === 'bucket_suffix') {
            bucketSuffix = '-' + split[1];
        }
        if (split[0] === 'dir') {
            outputDir = split[1];
        }
    }
});

if (isDebug) {
    if (!fs.existsSync(__dirname + './../generated/templates/policies/aws/')) {
        fs.mkdirSync(__dirname + './../generated/templates/policies/aws/', {recursive: true});
    }
    if (!fs.existsSync(__dirname + './../generated/templates/policies/awschina/')) {
        fs.mkdirSync(__dirname + './../generated/templates/policies/awschina/', {recursive: true});
    }
    if (!fs.existsSync(__dirname + './../generated/templates/policies/awsgov/')) {
        fs.mkdirSync(__dirname + './../generated/templates/policies/awsgov/', {recursive: true});
    }
    if (!fs.existsSync(__dirname + './../generated/templates/role_based/')) {
        fs.mkdirSync(__dirname + './../generated/templates/role_based/', {recursive: true});
    }
    if (!fs.existsSync(__dirname + './../generated/templates/user_based/')) {
        fs.mkdirSync(__dirname + './../generated/templates/user_based/', {recursive: true});
    }
    outputDir = __dirname + './..'
}

async function roleBasedOnboarding() {
    let orchestratorRole = yamlParse(fs.readFileSync(__dirname + '/../replacements/orchestartor_role.yml', 'utf8'));
    let onboardingJson = yamlParse(fs.readFileSync(__dirname + '/../role_based/onboarding.yml', 'utf8'));
    replaceObjectByPlaceholders(onboardingJson, [
        {key: 'REPLACEMENT_METADATA', value: metadata},
        {key: 'REPLACEMENT_PARAMETERS', value: parameters},
        {key: 'REPLACEMENT_CONDITIONS', value: conditions},
        {key: 'REPLACEMENT_SATCK_MODIFY_POLICY_STATEMENT', value: stackModifyPolicyStatements},
        {key: 'REPLACEMENT_SNS', value: sns},
        {key: 'REPLACEMENT_ORCHESTRATOR_ROLE', value: orchestratorRole},
        {key: 'REPLACEMENT_ORCHESTRATOR_ROLE_POLICY_STATEMENTS', value: roleBasedOrchestratorRolePolicies},
        {key: 'REPLACEMENT_ORCHESTRATOR', value: orchestrator},
        {key: 'REPLACEMENT_ORCHESTRATOR_HELPER', value: helperLambda},
        {key: 'REPLACEMENT_ORCHESTRATOR_INVOKE_PROPERTIES', value: orchestratorInvokeProperties},
        {key: 'REPLACEMENT_BUCKET_SUFFIX', value: bucketSuffix},
        {key: 'REPLACEMENT_ONBOARDING_TYPE', value: ROLE_BASED_ONBOARDING},
    ]);
    let onboardingYml = yamlDump(onboardingJson);
    writToFile('/generated/templates/role_based/onboarding.yml', onboardingYml);
    return {orchestratorRole, onboardingJson, onboardingYml};
}

async function roleBasedReadOnly() {
    let permissionsReadOnlyJson = yamlParse(fs.readFileSync(__dirname + '/../role_based/permissions_readonly_cft.yml', 'utf8'));
    replaceObjectByPlaceholders(permissionsReadOnlyJson, [
        {key: 'REPLACEMENT_READONLY_POLICY', value: readonlyPolicy},
    ]);
    let permissionsReadOnlyYml = yamlDump(permissionsReadOnlyJson);
    writToFile('/generated/templates/role_based/permissions_readonly_cft.yml', permissionsReadOnlyYml);
    return {permissionsReadOnlyJson, permissionsReadOnlyYml};
}

async function roleBasedReadWrite() {
    let permissionsReadwriteJson = yamlParse(fs.readFileSync(__dirname + '/../role_based/permissions_readwrite_cft.yml', 'utf8'));
    replaceObjectByPlaceholders(permissionsReadwriteJson, [
        {key: 'REPLACEMENT_READONLY_POLICY', value: readonlyPolicy},
        {key: 'REPLACEMENT_READWRITE_POLICY', value: readwritePolicy},
    ]);
    let permissionsReadwriteYml = yamlDump(permissionsReadwriteJson);
    writToFile('/generated/templates/role_based/permissions_readwrite_cft.yml', permissionsReadwriteYml)
    return {permissionsReadwriteJson, permissionsReadwriteYml};
}

async function roleBasedIntelligence() {
    let intelligence = fs.readFileSync(__dirname + '/../role_based/intelligence_cft.yml', 'utf8');
    writToFile('/generated/templates/role_based/intelligence_cft.yml', intelligence);
}

async function roleBasedServerless() {
    let serverlessJson = yamlParse(fs.readFileSync(__dirname + '/../role_based/serverless_cft.yml', 'utf8'));
    replaceObjectByPlaceholders(serverlessJson, [
        {key: 'REPLACEMENT_METADATA', value: metadata},
    ]);
    let serverlessYml = yamlDump(serverlessJson);
    writToFile('/generated/templates/role_based/serverless_cft.yml', serverlessYml);
}

async function roleBased() {
    // role based onboarding
    let {orchestratorRole, onboardingJson, onboardingYml} = await roleBasedOnboarding();

    // role based readonly
    let {permissionsReadOnlyJson, permissionsReadOnlyYml} = await roleBasedReadOnly();

    // role based readwrite
    let {permissionsReadwriteJson, permissionsReadwriteYml} = await roleBasedReadWrite();

    // role based intelligence
    await roleBasedIntelligence();

    // role based serverless
    await roleBasedServerless();

    return {
        orchestratorRole,
        onboardingJson,
        onboardingYml,
        permissionsReadOnlyJson,
        permissionsReadOnlyYml,
        permissionsReadwriteJson,
        permissionsReadwriteYml
    };
}

async function userBasedOnboarding(orchestratorRole, onboardingJson, onboardingYml) {
    orchestratorRole = yamlParse(fs.readFileSync(__dirname + '/../replacements/orchestartor_role.yml', 'utf8'));
    onboardingJson = yamlParse(fs.readFileSync(__dirname + '/../user_based/onboarding.yml', 'utf8'));
    replaceObjectByPlaceholders(onboardingJson, [
        {key: 'REPLACEMENT_METADATA', value: metadata},
        {key: 'REPLACEMENT_PARAMETERS', value: parameters},
        {key: 'REPLACEMENT_CONDITIONS', value: conditions},
        {key: 'REPLACEMENT_SATCK_MODIFY_POLICY_STATEMENT', value: stackModifyPolicyStatements},
        {key: 'REPLACEMENT_SNS', value: sns},
        {key: 'REPLACEMENT_ORCHESTRATOR_ROLE', value: orchestratorRole},
        {key: 'REPLACEMENT_ORCHESTRATOR_ROLE_POLICY_STATEMENTS', value: userBasedOrchestratorRolePolicies},
        {key: 'REPLACEMENT_ORCHESTRATOR', value: orchestrator},
        {key: 'REPLACEMENT_ORCHESTRATOR_HELPER', value: helperLambda},
        {key: 'REPLACEMENT_ORCHESTRATOR_INVOKE_PROPERTIES', value: orchestratorInvokeProperties},
        {key: 'REPLACEMENT_BUCKET_SUFFIX', value: bucketSuffix},
        {key: 'REPLACEMENT_ONBOARDING_TYPE', value: USER_BASED_ONBOARDING},
    ]);
    onboardingYml = yamlDump(onboardingJson);
    writToFile('/generated/templates/user_based/onboarding.yml', onboardingYml);
}

async function userBasedReadOnly(permissionsReadOnlyJson, permissionsReadOnlyYml) {
    permissionsReadOnlyJson = yamlParse(fs.readFileSync(__dirname + '/../user_based/permissions_readonly_cft.yml', 'utf8'));
    replaceObjectByPlaceholders(permissionsReadOnlyJson, [
        {key: 'REPLACEMENT_READONLY_POLICY', value: readonlyPolicy},
    ]);
    permissionsReadOnlyYml = yamlDump(permissionsReadOnlyJson);
    writToFile('/generated/templates/user_based/permissions_readonly_cft.yml', permissionsReadOnlyYml);
}

async function userBasedReadWrite(permissionsReadwriteJson, permissionsReadwriteYml) {
    permissionsReadwriteJson = yamlParse(fs.readFileSync(__dirname + '/../user_based/permissions_readwrite_cft.yml', 'utf8'));
    replaceObjectByPlaceholders(permissionsReadwriteJson, [
        {key: 'REPLACEMENT_READONLY_POLICY', value: readonlyPolicy},
        {key: 'REPLACEMENT_READWRITE_POLICY', value: readwritePolicy},
    ]);
    permissionsReadwriteYml = yamlDump(permissionsReadwriteJson);
    writToFile('/generated/templates/user_based/permissions_readwrite_cft.yml', permissionsReadwriteYml);
}

async function userBased(orchestratorRole, onboardingJson, onboardingYml, permissionsReadOnlyJson, permissionsReadOnlyYml, permissionsReadwriteJson, permissionsReadwriteYml) {
    // user based onboarding
    await userBasedOnboarding(orchestratorRole, onboardingJson, onboardingYml);

    // user based readonly
    await userBasedReadOnly(permissionsReadOnlyJson, permissionsReadOnlyYml);

    // user based readwrite
    await userBasedReadWrite(permissionsReadwriteJson, permissionsReadwriteYml);
}


async function replacer() {
    replaceObjectByPlaceholders(readonlyPolicy, [
        {key: 'REPLACEMENT_READONLY_POLICY_STATEMENTS', value: readonlyPolicyStatements},
    ]);

    let {
        orchestratorRole,
        onboardingJson,
        onboardingYml,
        permissionsReadOnlyJson,
        permissionsReadOnlyYml,
        permissionsReadwriteJson,
        permissionsReadwriteYml
    } = await roleBased();

    await userBased(orchestratorRole, onboardingJson, onboardingYml, permissionsReadOnlyJson, permissionsReadOnlyYml,
        permissionsReadwriteJson, permissionsReadwriteYml);

    await createPolicyJsonFiles(readwritePolicy);
}

function removeFnSub(element) {
    if (element == null) {
        throw new Error('element is null');
    }

    for (const value of element?.Statement) {
        if (value?.NotResource){
            value.NotResource = value.NotResource["Fn::Sub"];
        }
    }
    return element;
}

async function createPolicyJsonFiles(readwritePolicy){
    const allTasks = [];

    // aws
    let readonlyPolicyStatementsJson = yamlParse(fs.readFileSync(__dirname + '/../replacements/readonly_policy_statements.yml', 'utf8'));
    let readonlyPolicyJson = yamlParse(fs.readFileSync(__dirname + '/../replacements/readonly_policy.yml', 'utf8'));
    let readonlyPolicyJsonWithoutFn = removeFnSub(readonlyPolicyJson);
    allTasks.push(createPolicyJsonFilesAws(readonlyPolicyJsonWithoutFn, readonlyPolicyStatementsJson, readwritePolicy));

    // aws-cn
    readonlyPolicyStatementsJson = yamlParse(fs.readFileSync(__dirname + '/../replacements/readonly_policy_statements.yml', 'utf8'));
    readonlyPolicyJson = yamlParse(fs.readFileSync(__dirname + '/../replacements/readonly_policy_china.yml', 'utf8'));
    readonlyPolicyJsonWithoutFn = removeFnSub(readonlyPolicyJson);
    allTasks.push(createPolicyJsonFilesAwsChina(readonlyPolicyJsonWithoutFn, readonlyPolicyStatementsJson, readwritePolicy));

    // aws-us-gov
    readonlyPolicyStatementsJson = yamlParse(fs.readFileSync(__dirname + '/../replacements/readonly_policy_statements.yml', 'utf8'));
    readonlyPolicyJson = yamlParse(fs.readFileSync(__dirname + '/../replacements/readonly_policy.yml', 'utf8'));
    readonlyPolicyJsonWithoutFn = removeFnSub(readonlyPolicyJson);
    allTasks.push(createPolicyJsonFilesAwsGov(readonlyPolicyJsonWithoutFn, readonlyPolicyStatementsJson, readwritePolicy));

    await Promise.all(allTasks);
}

function replaceObjectByPlaceholders(element, replacements) { // replacementKey, replacementValue){
    for (let replacement of replacements) {
        replaceObjectByPlaceholder(element, replacement.key, replacement.value);
    }
}

function replaceObjectByPlaceholder(element, replacementKey, replacementValue) {

    if (element == null) {
        return;
    }

    for (const [key, value] of Object.entries(element)) {
        if (key === replacementKey) {
            let aa = {};
            for (const [k, v] of Object.entries(element)) {
                if (k === replacementKey) {
                    Object.assign(aa, replacementValue);
                } else {
                    aa[k] = v;
                }
                delete element[k];

            }
            Object.assign(element, aa);
            continue;
        }

        if (typeof value === 'object' || Array.isArray(value)) {
            replaceObjectByPlaceholder(value, replacementKey, replacementValue)
            continue;
        }
        if (typeof value !== 'string') {
            continue;
        }
        if (value.includes(replacementKey)) {
            if (typeof replacementValue == 'string') {
                element[key] = value.replace(replacementKey, replacementValue)
            } else if (Array.isArray(element)) {
                element.splice(Number(key), 1);
                if (replacementValue == null) {
                    continue;
                }
                if (Array.isArray(replacementValue)) {
                    element.push(...replacementValue);
                } else {
                    element.push(replacementValue);
                }
            } else {
                element[key] = replacementValue;
            }
        }
    }
}

async function createPolicyJsonFilesAws(readonlyPolicyJson, readonlyPolicyStatementsJson, readwritePolicy) {
    const awsPartitionValue = 'aws';
    replaceObjectByPlaceholders(readonlyPolicyJson, [
        {key: 'REPLACEMENT_READONLY_POLICY_STATEMENTS', value: readonlyPolicyStatementsJson},
        {key: 'REPLACEMENT_POLICY_PARTITION', value: "aws"}
    ]);
    writToFile('/generated/templates/policies/aws/readonly_policy.json', JSON.stringify(readonlyPolicyJson, null, 4).replace(AWS_PARTITION, awsPartitionValue));
    writToFile('/generated/templates/policies/aws/readwrite_policy.json', JSON.stringify(readwritePolicy, null, 4));
}

async function createPolicyJsonFilesAwsChina(readonlyPolicyJson, readonlyPolicyStatementsJson, readwritePolicy) {
    const awsPartitionValue = 'aws-cn';
    replaceObjectByPlaceholders(readonlyPolicyJson, [
        {key: 'REPLACEMENT_READONLY_POLICY_STATEMENTS', value: readonlyPolicyStatementsJson},
        {key: 'REPLACEMENT_POLICY_PARTITION', value: "aws-cn"}
    ]);
    writToFile('/generated/templates/policies/awschina/readonly_policy.json', JSON.stringify(readonlyPolicyJson, null, 4).replace(AWS_PARTITION, awsPartitionValue));
    writToFile('/generated/templates/policies/awschina/readwrite_policy.json', JSON.stringify(readwritePolicy, null, 4));
}

async function createPolicyJsonFilesAwsGov(readonlyPolicyJson, readonlyPolicyStatementsJson, readwritePolicy) {
    const awsPartitionValue = 'aws-us-gov';
    replaceObjectByPlaceholders(readonlyPolicyJson, [
        {key: 'REPLACEMENT_READONLY_POLICY_STATEMENTS', value: readonlyPolicyStatementsJson},
        {key: 'REPLACEMENT_POLICY_PARTITION', value: "aws-us-gov"}
    ]);
    writToFile('/generated/templates/policies/awsgov/readonly_policy.json', JSON.stringify(readonlyPolicyJson, null, 4).replace(AWS_PARTITION, awsPartitionValue));
    writToFile('/generated/templates/policies/awsgov/readwrite_policy.json', JSON.stringify(readwritePolicy, null, 4));
}

function writToFile(path, content) {
    if (outputDir != null) {
        fs.writeFileSync(outputDir + path, content);
        console.log('wrote file to ' + outputDir + path);
    }

}


const main = async () => {
    try {
        console.log('\n\nStart Replacer \n');
        await replacer();
        console.log('\n\nFinished Replacer Successfully \n');

    } catch (e) {
        console.log(e);
        throw e;
    }
}

main();
















