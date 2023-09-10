import json
import click
import requests
import subprocess

SCAN_MODES = {
    "SAAS": 'saas',
    "IN_ACCOUNT": 'inAccount'
}

DEFAULT_ENV_REGION = "us-east-1"
DEFAULT_SCAN_MODE = SCAN_MODES["SAAS"]

ADD_ACCOUNT_API = "/workload/agentless/azure/accounts/{id}/enable/"
GET_ONBOARDING_DATA_API = "/workload/agentless/azure/accounts/{id}/onboarding?scanMode={scan_mode}"
GET_CLOUD_ACCOUNT = "/AzureCloudAccount/{id}"

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


def get_onboarding_data(base_url, api_key, api_secret, azure_subscription_id, scan_mode):
    headers = {
        'Accept': 'application/json'
    }
    url = f"{base_url}{GET_ONBOARDING_DATA_API.format(id=azure_subscription_id, scan_mode=scan_mode)}"
    response = requests.request("GET", url, headers=headers, auth=(api_key, api_secret))
    print("Get onboarding data status code: {} ".format(response.status_code))
    if response.status_code != 200:
        raise Exception("Failed to get onboarding data, content: {}".format(str(response.content)))
    return json.loads(response.content)


def add_account(base_url, azure_subscription_id, api_key, api_secret, scan_mode):
    payload = {
        'scanMode': scan_mode
    }
    headers = {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
    }
    url = f"{base_url}{ADD_ACCOUNT_API.format(id=azure_subscription_id)}"
    response = requests.request("POST", url, headers=headers, auth=(api_key, api_secret), json=payload)
    print("Add account status code: {} ".format(response.status_code))
    if response.status_code != 201:
        raise Exception("Failed to add_account, content: {}".format(str(response.content)))


def onboarding(base_url, azure_subscription_id, api_key, api_secret, scan_mode):
    # 1. get awp onboarding data
    print("get awp onboarding data")
    azure_onboarding_data = get_onboarding_data(base_url, api_key, api_secret, azure_subscription_id, scan_mode)
    onboarding_script_command = azure_onboarding_data.get('onboardingScriptCommand')
    print(f"onboardingScriptCommand: {onboarding_script_command}")

    # 2. run onboarding script
    try:
        print("Running AWP onboarding script")
        completed_process = subprocess.run(onboarding_script_command, shell=True)
        if completed_process.returncode != 0:
            raise Exception("Failed to run onboarding script, make sure you run in Azure Cloud Shell and all parameters are valid")
        print("AWP onboarding script completed successfully")
    except Exception as e:
        raise Exception("Failed to run onboarding script, error: {}".format(str(e)))

    # 3. call to awp add account api
    print("Create AWP account")
    add_account(base_url, azure_subscription_id, api_key, api_secret, scan_mode)
    print(f"AWP account created successfully - {azure_subscription_id}")


@click.command()
@click.option("-c", "--cloud_account_id", required=True, help="CloudGuard account id")
@click.option("-k", "--api_key", required=True, help="CloudGuard API key")
@click.option("-s", "--api_secret", required=True, help="CloudGuard API secret key")
@click.option("-m", "--scan_mode", default=DEFAULT_SCAN_MODE, required=False, help="AWP scan mode (saas / inAccount)")
@click.option("-r", "--cloudguard_region", default=DEFAULT_ENV_REGION, required=False, help="CloudGuard environment region")
@click.option("-b", "--cloudguard_base_url", default=None, required=False, help="CloudGuard environment base URL")
def onboarding_awp(cloud_account_id, api_key, api_secret, scan_mode, cloudguard_region, cloudguard_base_url):
    if not cloudguard_base_url:
        base_url = ENVS_REGION_PARAMS_MAP.get(cloudguard_region, {}).get('base_url')
    else:
        base_url = cloudguard_base_url.rstrip('/')

    cloud_account_data = get_cloud_account_data(base_url, api_key, api_secret, cloud_account_id)
    if not cloud_account_data:
        raise Exception("Account validation failed, make sure the account is onboarded to cloudguard and that your api key and secret are valid.")

    azure_subscription_id = cloud_account_data['subscriptionId']
    print(f"azure_subscription_id: {azure_subscription_id}")

    onboarding(base_url, azure_subscription_id, api_key, api_secret, scan_mode)


if __name__ == '__main__':
    onboarding_awp()
