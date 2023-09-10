# Onboarding - CloudGuard AWP

This script will enable AWP for AWS cloud account, and will deploy a new CloudForamtion stack for AWP.

The account must be onboarded to CloudGuard before running the script.

### Install
```shell
pip3 install -r requirements.txt
```

### Run
```shell
python3 onboarding_awp.py --cloud_account_id <cloudguard-account-id> --api_key <cloudguard-api-key> --api_secret <cloudguard-api-secret> --scan_mode <scan-mode> --profile <aws-account-profile>
```
