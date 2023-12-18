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

## WIKI:
https://wiki.checkpoint.com/confluence/display/GlobalPO/CloudGuard+-+Unified+Onboarding




