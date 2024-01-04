# Unified Onboarding

### This repository contains code for the CloudGuard Aws Unified Onboarding

## What is Unified Onboarding

CloudGuard has multiple separate modules, Inventory + Posture, Intelligence and Serverless.<br>
Till now, there were a separate and manual onboarding process for each module.<br>
The Unified Onboarding is here to solve this two problems, it gives the option to onboard all moduls at once,
and in one simple click by running a CFT on your environment.

## How does it works

You reach out CloudGuard with the configuration of which modules you want to onboard, then
you get a link for the AWS CloudFormation console, then you just need to run the CFT to get onboarded.<br>
The CFT will create a lambda that will onboard all the selected modules into CloudGuard.

> [!NOTE]
>  Lambda is deleted once the CFT deployment completes.


## Policies
> [!IMPORTANT]
> Those policies required for Data fetching, The metadata is used in Inventory and compliance modules.

### Required policies <br>
#### **AWS** <br>
**SecurityAudit (AWS managed policy)** - Mandatory - The system relies on most of the actions <br>
**ReadOnlyAccess (AWS managed policy)** - Optional - An extension to the SecurityAudit policy, Reduce the effort to constantly update the CloudGuard-readonly-policy whenever we add newer entities support  <br>
[CloudGuard-readonly-policy](https://cloudguard-unified-onboarding-us-east-1.s3.amazonaws.com/unified-onboarding/current/templates/policies/aws/readonly_policy.json) - Mandatory - An extension to the SecurityAudit policy, contains minimum required actions  <br>
[CloudGuard-write-policy](https://cloudguard-unified-onboarding-us-east-1.s3.amazonaws.com/unified-onboarding/current/templates/policies/aws/readwrite_policy.json) - Optional - Required for network security management actions.  <br>

#### **AWS-China** <br>
**SecurityAudit (AWS managed policy)** - Mandatory - The system relies on most of the actions <br>
**ReadOnlyAccess (AWS managed policy)** - Optional - An extension to the SecurityAudit policy, Reduce the effort to constantly update the CloudGuard-readonly-policy whenever we add newer entities support  <br>
[CloudGuard-readonly-policy](https://cloudguard-unified-onboarding-us-east-1.s3.amazonaws.com/unified-onboarding/current/templates/policies/awschina/readonly_policy.json) - Mandatory - An extension to the SecurityAudit policy, contains minimum required actions  <br>
[CloudGuard-write-policy](https://cloudguard-unified-onboarding-us-east-1.s3.amazonaws.com/unified-onboarding/current/templates/policies/awschina/readwrite_policy.json) - Optional - Required for network security management actions.  <br>


#### **AWS-Gov** <br>
**SecurityAudit (AWS managed policy)** - Mandatory - The system relies on most of the actions <br>
**ReadOnlyAccess (AWS managed policy)** - Optional - An extension to the SecurityAudit policy, Reduce the effort to constantly update the CloudGuard-readonly-policy whenever we add newer entities support  <br>
[CloudGuard-readonly-policy](https://cloudguard-unified-onboarding-us-east-1.s3.amazonaws.com/unified-onboarding/current/templates/policies/awsgov/readonly_policy.json) - Mandatory - An extension to the SecurityAudit policy, contains minimum required actions  <br>
[CloudGuard-write-policy](https://cloudguard-unified-onboarding-us-east-1.s3.amazonaws.com/unified-onboarding/current/templates/policies/awsgov/readwrite_policy.json) - Optional - Required for network security management actions.  <br>

## WIKI:
https://wiki.checkpoint.com/confluence/display/GlobalPO/CloudGuard+-+Unified+Onboarding

Testing: <br>
https://wiki.checkpoint.com/confluence/pages/viewpage.action?spaceKey=GlobalPO&title=Testing+-+CloudGuard+-+Unified+Onboarding