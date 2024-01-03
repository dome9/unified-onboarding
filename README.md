# Unified Onboarding

### This repository contains code for the CloudGuard Aws (for now) Unified Onboarding

## What is Unified Onboarding?

CloudGuard has multiple separate modules, Inventory + Posture, Intelligence and Serverless.<br>
Till now, there were a separate and manual onboarding process for each module.<br>
The Unified Onboarding is here to solve this two problems, it gives the option to onboard all moduls at once, 
and in one simple click by running a CFT on your environment.

## How does it works? 

You reach out CloudGuard with the configuration of which modules you want to onboard, then
you get a link for the AWS CloudFormation console, then you just need to run the CFT to get onboarded.<br>
The CFT will create a lambda that will onboard all the selected modules into CloudGuard.

**NOTE:** Lambda is deleted once the CFT deployment completes.

## Policies:
**AWS**: <br>
https://cloudguard-unified-onboarding-us-east-1.s3.amazonaws.com/unified-onboarding/current/templates/policies/aws/readonly_policy.json
<br>
https://cloudguard-unified-onboarding-us-east-1.s3.amazonaws.com/unified-onboarding/current/templates/policies/aws/readwrite_policy.json

**AWS-China**: <br>
https://cloudguard-unified-onboarding-us-east-1.s3.amazonaws.com/unified-onboarding/current/templates/policies/awschina/readonly_policy.json
<br>
https://cloudguard-unified-onboarding-us-east-1.s3.amazonaws.com/unified-onboarding/current/templates/policies/awschina/readwrite_policy.json

**AWS-Gov**: <br>
https://cloudguard-unified-onboarding-us-east-1.s3.amazonaws.com/unified-onboarding/current/templates/policies/awsgov/readonly_policy.json
<br>
https://cloudguard-unified-onboarding-us-east-1.s3.amazonaws.com/unified-onboarding/current/templates/policies/awsgov/readwrite_policy.json

## WIKI:
https://wiki.checkpoint.com/confluence/display/GlobalPO/CloudGuard+-+Unified+Onboarding

Testing: <br>
https://wiki.checkpoint.com/confluence/pages/viewpage.action?spaceKey=GlobalPO&title=Testing+-+CloudGuard+-+Unified+Onboarding



