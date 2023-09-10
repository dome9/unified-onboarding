import base64
import json
import boto3
import click
import requests

SCAN_MODES = {
    "SAAS": 'saas',
    "IN_ACCOUNT": 'inAccount'
}

DEFAULT_ENV_REGION = "us-east-1"
DEFAULT_SCAN_MODE = SCAN_MODES["SAAS"]

AWP_STACK_NAME = "CloudguardAWPCrossAccountStack"
AWP_ROLE_NAME = "CloudGuardAWPCrossAccountRole"

ADD_ACCOUNT_API = "/workload/agentless/aws/accounts/{id}/enable/"
GET_ONBOARDING_DATA_API = "/workload/agentless/aws/onboarding/"

GET_CLOUD_ACCOUNT = "/CloudAccounts/{id}"
GET_CLOUDGUARD_APP_DATA = "/application/d9awsaccount"

ENVS_REGION_PARAMS_MAP = {
    "us-east-1": {
        "base_url": "https://api.dome9.com/v2"
    },
    "eu-west-1": {
        "base_url": "https://api.eu1.dome9.com/v2"
    },
    "ap-southeast-1": {
        "base_url": "https://api.ap1.dome9.com/v2"
    },
    "ap-southeast-2": {
        "base_url": "https://api.ap2.dome9.com/v2"
    },
    "ap-south-1": {
        "base_url": "https://api.ap3.dome9.com/v2"
    },
    "ca-central-1": {
        "base_url": "https://api.cace1.dome9.com/v2"
    }
}


def get_cloud_account_data(base_url, api_key, api_secret, cloud_account_id):
    headers = {
        'Accept': 'application/json'
    }
    url = f"{base_url}{GET_CLOUD_ACCOUNT.format(id=cloud_account_id)}"
    try:
        print("Validating account...")
        response = requests.request("GET", url, headers=headers, auth=(api_key, api_secret))
        print("Validate account status code: {} ".format(response.status_code))
        if response.status_code != 200:
            return False
        return json.loads(response.content)
    except:
        print("Account validation failed, please make sure you using a valid base-url and try again...")
        exit(1)


def get_cloudguard_env_account_id(base_url, api_key, api_secret):
    headers = {
        'Accept': 'application/json'
    }
    url = f"{base_url}{GET_CLOUDGUARD_APP_DATA}"
    response = requests.request("GET", url, headers=headers, auth=(api_key, api_secret))
    print("Get onboarding data status code: {} ".format(response.status_code))
    if response.status_code != 200:
        raise Exception("Failed to get onboarding data, content: {}".format(str(response.content)))
    return json.loads(response.content)["d9AwsAccountNumber"]


def get_onboarding_data(base_url, api_key, api_secret):
    headers = {
        'Accept': 'application/json'
    }
    url = f"{base_url}{GET_ONBOARDING_DATA_API}"
    response = requests.request("GET", url, headers=headers, auth=(api_key, api_secret))
    print("Get onboarding data status code: {} ".format(response.status_code))
    if response.status_code != 200:
        raise Exception("Failed to get onboarding data, content: {}".format(str(response.content)))
    return json.loads(response.content)


def add_account(base_url, cloud_account_id, api_key, api_secret, scan_mode, cross_account_external_id):
    payload = {
        'crossAccountRoleExternalId': cross_account_external_id,
        'crossAccountRoleName': AWP_ROLE_NAME,
        'scanMode': scan_mode
    }
    headers = {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
    }
    url = f"{base_url}{ADD_ACCOUNT_API.format(id=cloud_account_id)}"
    response = requests.request("POST", url, headers=headers, auth=(api_key, api_secret), json=payload)
    print("Add account status code: {} ".format(response.status_code))
    if response.status_code != 201:
        raise Exception("Failed to add_account, content: {}".format(str(response.content)))


def create_cross_account_stack(cf_client, stack_name, template_url, parameters):
    cf_client.create_stack(StackName=stack_name, TemplateURL=template_url, Capabilities=["CAPABILITY_NAMED_IAM"], Parameters=parameters)
    return


def wait_for_stack_creation(cf_client, stack_name):
    waiter = cf_client.get_waiter('stack_create_complete')
    waiter.wait(StackName=stack_name)
    return


def create_stack_params(cloudguard_env_account_id, cross_account_external_id, scan_mode):
    return [
        {
            'ParameterKey': 'CrossAccountExternalId',
            'ParameterValue': cross_account_external_id
        },
        {
            'ParameterKey': 'ScanMode',
            'ParameterValue': scan_mode
        },
        {
            'ParameterKey': 'CloudguardAccountId',
            'ParameterValue': cloudguard_env_account_id
        }
    ]


def onboarding(base_url, cf_client, aws_cloud_account_id, cloud_account_id, api_key, api_secret, cloudguard_env_account_id, scan_mode, cross_account_external_id):
    # 1. get awp stack template
    print("get awp cloudformation stack url")
    aws_onboarding_data = get_onboarding_data(base_url, api_key, api_secret)
    template_url = aws_onboarding_data['crossAccountTemplateUrl']
    print(f"template_url: {template_url}")

    # 2. create cross account stack
    print("Create cross account stack")
    parameters = create_stack_params(cloudguard_env_account_id, cross_account_external_id, scan_mode)
    create_cross_account_stack(cf_client, AWP_STACK_NAME, template_url, parameters)

    # 3. wait for created stack
    print("AWP cloudformation stack creation in progress - this may take a few minutes, please wait...")
    wait_for_stack_creation(cf_client, AWP_STACK_NAME)
    print("AWP stack created successfully")

    # 4. call to awp add account api
    print("Create AWP account")
    add_account(base_url, cloud_account_id, api_key, api_secret, scan_mode, cross_account_external_id)
    print(f"AWP account created successfully - {aws_cloud_account_id}")


@click.command()
@click.option("-c", "--cloud_account_id", required=True, help="CloudGuard account id")
@click.option("-k", "--api_key", required=True, help="CloudGuard API key")
@click.option("-s", "--api_secret", required=True, help="CloudGuard API secret key")
@click.option("-m", "--scan_mode", default=DEFAULT_SCAN_MODE, required=False, help="AWP scan mode (saas / inAccount)")
@click.option("-p", "--profile", default=None, required=False, help="AWS Profile")
@click.option("-r", "--cloudguard_region", default=DEFAULT_ENV_REGION, required=False, help="CloudGuard environment region")
@click.option("-b", "--cloudguard_base_url", default=None, required=False, help="CloudGuard environment base URL")
@click.option("-a", "--cloudguard_env_account_id", default=None, required=False, help="CloudGuard environment account id")
def onboarding_awp(cloud_account_id, api_key, api_secret, scan_mode, profile, cloudguard_region, cloudguard_base_url, cloudguard_env_account_id):
    if not cloudguard_base_url:
        base_url = ENVS_REGION_PARAMS_MAP.get(cloudguard_region, {}).get('base_url')
    else:
        base_url = cloudguard_base_url.rstrip('/')

    cloud_account_data = get_cloud_account_data(base_url, api_key, api_secret, cloud_account_id)
    if not cloud_account_data:
        raise Exception("Account validation failed, make sure the account is onboarded to cloudguard and that your api key and secret are valid.")

    if not cloudguard_env_account_id:
        print("get cloudguard_env_account_id")
        cloudguard_env_account_id = get_cloudguard_env_account_id(base_url, api_key, api_secret)
        print(f"cloudguard_env_account_id: {cloudguard_env_account_id}")

    aws_cloud_account_id = cloud_account_data['externalAccountNumber']
    cloud_account_id = cloud_account_data['id']
    session_params = {}
    if profile:
        print("Profile is {}".format(profile))
        session_params["profile_name"] = profile
    print("Region is {}".format(cloudguard_region))
    session_params["region_name"] = cloudguard_region
    print("Using session params {}".format(str(session_params)))

    session = boto3.session.Session(**session_params)
    cf_client = session.client('cloudformation', region_name=cloudguard_region)

    cross_account_external_id_str = f"{cloudguard_env_account_id}-{cloud_account_id}".encode("utf-8")
    cross_account_external_id = base64.b64encode(cross_account_external_id_str).decode("utf-8")

    onboarding(base_url, cf_client, aws_cloud_account_id, cloud_account_id, api_key, api_secret, cloudguard_env_account_id, scan_mode, cross_account_external_id)


if __name__ == '__main__':
    onboarding_awp()
