# Onboarding - CloudGuard AWP

This script will enable AWP for Azure subscription.

The subscription must be onboarded to CloudGuard before running the script.

### Install
```shell
pip3 install -r requirements.txt
```

### Run
In order to run the onboarding script for AWP, you need to run the following script in Azure Cloud Shell or with AZ CLI in your terminal:
```shell
python3 onboarding_awp.py --cloud_account_id <cloudguard-account-id> --api_key <cloudguard-api-key> --api_secret <cloudguard-api-secret> --scan_mode <scan-mode>
```
