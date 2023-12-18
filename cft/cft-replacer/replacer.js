const fs = require('fs');
const {yamlParse, yamlDump} = require('yaml-cfn');
const {argv} = require('process');
const isDebug = process.env.isDebug || false
let bucketSuffix = '';
let outputDir = '';

argv.forEach((val, index) => {
    let split = val.split('=');
    if (split.length === 2) {
        if (split[0] === 'bucket_suffix') {
            bucketSuffix = '-' + split[1]
        }
        if (split[0] === 'dir') {
            outputDir = split[1]
        }
    }
});

if (isDebug) {
    if (!fs.existsSync(__dirname + './../generated/templates/policies/')) {
        fs.mkdirSync(__dirname + './../generated/templates/policies/', {recursive: true})
    }
    if (!fs.existsSync(__dirname + './../generated/templates/role_based/')) {
        fs.mkdirSync(__dirname + './../generated/templates/role_based/', {recursive: true})
    }
    if (!fs.existsSync(__dirname + './../generated/templates/user_based/')) {
        fs.mkdirSync(__dirname + './../generated/templates/user_based/', {recursive: true})
    }
    outputDir = __dirname + './..'
}

let replacer = function () {
    try {
        const orchestrator = yamlParse(fs.readFileSync(__dirname + '/../replacements/orchestrator.yml', 'utf8'))
        const orchestratorInvokeProperties = yamlParse(fs.readFileSync(__dirname + '/../replacements/orchestrator_invoke_properties.yml', 'utf8'))
        const parameters = yamlParse(fs.readFileSync(__dirname + '/../replacements/parameters.yml', 'utf8'))
        const readonlyPolicy = yamlParse(fs.readFileSync(__dirname + '/../replacements/readonly_policy.yml', 'utf8'))
        const readonlyPolicyStatements = yamlParse(fs.readFileSync(__dirname + '/../replacements/readonly_policy_statements_cft.yml', 'utf8'))
        const readwritePolicy = yamlParse(fs.readFileSync(__dirname + '/../replacements/readwrite_policy.yml', 'utf8'))
        const stackModifyPolicyStatements = yamlParse(fs.readFileSync(__dirname + '/../replacements/stack_modify_policy_statements.yml', 'utf8'))
        const metadata = yamlParse(fs.readFileSync(__dirname + '/../replacements/metadata.yml', 'utf8'))
        const userBasedOrchestratorRolePolicies = yamlParse(fs.readFileSync(__dirname + '/../replacements/user_based_orchestrator_role_policy_statements.yml', 'utf8'))
        const roleBasedOrchestratorRolePolicies = yamlParse(fs.readFileSync(__dirname + '/../replacements/role_based_orchestrator_role_policy_statements.yml', 'utf8'))
        const sns = yamlParse(fs.readFileSync(__dirname + '/../replacements/sns.yml', 'utf8'))
        const helperLambda = yamlParse(fs.readFileSync(__dirname + '/../replacements/orchestrator_helper.yml', 'utf8'))
        const conditions = yamlParse(fs.readFileSync(__dirname + '/../replacements/conditions.yml', 'utf8'))
        const denyActionsPolicyStatement = yamlParse(fs.readFileSync(__dirname + '/../replacements/deny_actions_policy_statement.yml', 'utf8'))
        const roleBasedOnboarding = 'Role';
        const userBasedOnboarding = 'User';

        replaceObjectByPlaceholders(readonlyPolicy, [
            {key: 'REPLACEMENT_READONLY_POLICY_STATEMENTS', value: readonlyPolicyStatements},
        ]);

        // role based onboarding
        let orchestratorRole = yamlParse(fs.readFileSync(__dirname + '/../replacements/orchestartor_role.yml', 'utf8'))
        let onboardingJson = yamlParse(fs.readFileSync(__dirname + '/../role_based/onboarding.yml', 'utf8'))
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
            {key: 'REPLACEMENT_ONBOARDING_TYPE', value: roleBasedOnboarding},
        ]);
        let onboardingYml = yamlDump(onboardingJson)
        writToFile('/generated/templates/role_based/onboarding.yml', onboardingYml)

        // role based readonly
        let permissionsReadOnlyJson = yamlParse(fs.readFileSync(__dirname + '/../role_based/permissions_readonly_cft.yml', 'utf8'))
        replaceObjectByPlaceholders(permissionsReadOnlyJson, [
            {key: 'REPLACEMENT_READONLY_POLICY', value: readonlyPolicy},
            {key: 'REPLACEMENT_ACTIONS_POLICY_STATEMENT', value: denyActionsPolicyStatement},
        ]);
        let permissionsReadOnlyYml = yamlDump(permissionsReadOnlyJson)
        writToFile('/generated/templates/role_based/permissions_readonly_cft.yml', permissionsReadOnlyYml)

        // role based readwrite
        let permissionsReadwriteJson = yamlParse(fs.readFileSync(__dirname + '/../role_based/permissions_readwrite_cft.yml', 'utf8'))
        replaceObjectByPlaceholders(permissionsReadwriteJson, [
            {key: 'REPLACEMENT_READONLY_POLICY', value: readonlyPolicy},
            {key: 'REPLACEMENT_READWRITE_POLICY', value: readwritePolicy},
            {key: 'REPLACEMENT_ACTIONS_POLICY_STATEMENT', value: denyActionsPolicyStatement},
        ]);
        let permissionsReadwriteYml = yamlDump(permissionsReadwriteJson)
        writToFile('/generated/templates/role_based/permissions_readwrite_cft.yml', permissionsReadwriteYml)

        // role based intelligence
        let intelligence = fs.readFileSync(__dirname + '/../role_based/intelligence_cft.yml', 'utf8')
        writToFile('/generated/templates/role_based/intelligence_cft.yml', intelligence)

        // role based serverless
        let serverlessJson = yamlParse(fs.readFileSync(__dirname + '/../role_based/serverless_cft.yml', 'utf8'))
        replaceObjectByPlaceholders(serverlessJson, [
            {key: 'REPLACEMENT_METADATA', value: metadata},
        ]);
        let serverlessYml = yamlDump(serverlessJson)
        writToFile('/generated/templates/role_based/serverless_cft.yml', serverlessYml)


        // user based onboarding
        orchestratorRole = yamlParse(fs.readFileSync(__dirname + '/../replacements/orchestartor_role.yml', 'utf8'))
        onboardingJson = yamlParse(fs.readFileSync(__dirname + '/../user_based/onboarding.yml', 'utf8'))
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
            {key: 'REPLACEMENT_ONBOARDING_TYPE', value: userBasedOnboarding},
        ]);
        onboardingYml = yamlDump(onboardingJson)
        writToFile('/generated/templates/user_based/onboarding.yml', onboardingYml)

        // user based readonly
        permissionsReadOnlyJson = yamlParse(fs.readFileSync(__dirname + '/../user_based/permissions_readonly_cft.yml', 'utf8'))
        replaceObjectByPlaceholders(permissionsReadOnlyJson, [
            {key: 'REPLACEMENT_READONLY_POLICY', value: readonlyPolicy},
            {key: 'REPLACEMENT_ACTIONS_POLICY_STATEMENT', value: denyActionsPolicyStatement},
        ]);
        permissionsReadOnlyYml = yamlDump(permissionsReadOnlyJson)
        writToFile('/generated/templates/user_based/permissions_readonly_cft.yml', permissionsReadOnlyYml)

        // user based readwrite
        permissionsReadwriteJson = yamlParse(fs.readFileSync(__dirname + '/../user_based/permissions_readwrite_cft.yml', 'utf8'))
        replaceObjectByPlaceholders(permissionsReadwriteJson, [
            {key: 'REPLACEMENT_READONLY_POLICY', value: readonlyPolicy},
            {key: 'REPLACEMENT_READWRITE_POLICY', value: readwritePolicy},
            {key: 'REPLACEMENT_ACTIONS_POLICY_STATEMENT', value: denyActionsPolicyStatement},
        ]);
        permissionsReadwriteYml = yamlDump(permissionsReadwriteJson)
        writToFile('/generated/templates/user_based/permissions_readwrite_cft.yml', permissionsReadwriteYml)

        // create policy json files
        // aws
        let readonlyPolicyStatementsJson = yamlParse(fs.readFileSync(__dirname + '/../replacements/readonly_policy_statements.yml', 'utf8'))
        let readonlyPolicyJson = yamlParse(fs.readFileSync(__dirname + '/../replacements/readonly_policy.yml', 'utf8'))
        replaceObjectByPlaceholders(readonlyPolicyJson, [
            {key: 'REPLACEMENT_READONLY_POLICY_STATEMENTS', value: readonlyPolicyStatementsJson},
            {key: 'REPLACEMENT_POLICY_PARTITION', value: "aws"}
        ]);
        writToFile('/generated/templates/policies/aws/readonly_policy.json', JSON.stringify(readonlyPolicyJson, null, 4))
        writToFile('/generated/templates/policies/aws/readwrite_policy.json', JSON.stringify(readwritePolicy, null, 4))

        // aws-cn
        readonlyPolicyStatementsJson = yamlParse(fs.readFileSync(__dirname + '/../replacements/readonly_policy_statements.yml', 'utf8'))
        readonlyPolicyJson = yamlParse(fs.readFileSync(__dirname + '/../replacements/readonly_policy_china.yml', 'utf8'))
        replaceObjectByPlaceholders(readonlyPolicyJson, [
            {key: 'REPLACEMENT_READONLY_POLICY_STATEMENTS', value: readonlyPolicyStatementsJson},
            {key: 'REPLACEMENT_POLICY_PARTITION', value: "aws-cn"}
        ]);
        writToFile('/generated/templates/policies/awschina/readonly_policy.json', JSON.stringify(readonlyPolicyJson, null, 4))
        writToFile('/generated/templates/policies/awschina/readwrite_policy.json', JSON.stringify(readwritePolicy, null, 4))

        // aws-us-gov
        readonlyPolicyStatementsJson = yamlParse(fs.readFileSync(__dirname + '/../replacements/readonly_policy_statements.yml', 'utf8'))
        readonlyPolicyJson = yamlParse(fs.readFileSync(__dirname + '/../replacements/readonly_policy.yml', 'utf8'))
        replaceObjectByPlaceholders(readonlyPolicyJson, [
            {key: 'REPLACEMENT_READONLY_POLICY_STATEMENTS', value: readonlyPolicyStatementsJson},
            {key: 'REPLACEMENT_POLICY_PARTITION', value: "aws-us-gov"}
        ]);
        writToFile('/generated/templates/policies/awsgov/readonly_policy.json', JSON.stringify(readonlyPolicyJson, null, 4))
        writToFile('/generated/templates/policies/awsgov/readwrite_policy.json', JSON.stringify(readwritePolicy, null, 4))
    } catch (e) {
        console.log(e);
        throw e;
    }
}

function replaceObjectByPlaceholders(element, replacements) { // replacementKey, replacementValue){
    for (let replacement of replacements) {
        replaceObjectByPlaceholder(element, replacement.key, replacement.value)
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

function writToFile(path, content) {
    if (outputDir != null) {
        fs.writeFileSync(outputDir + path, content)
        console.log('wrote file to ' + outputDir + path)
    }

}

replacer()

















