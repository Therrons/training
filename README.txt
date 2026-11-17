System Detail and Overview
==========================
The 'SOLUTION_OVERVIEW.md' document describes how The Fraud Detection PoC consumes categorised transaction events from Kafka, 
scores each one against five configurable fraud rules, and stores the flagged and unflagged results in PostgreSQL, 
where they can be queried through a JWT-protected REST API. 
The overview explains this in detail, covering the processing flow, validation, schema and idempotency, API contract, 
configuration, observability, testing and deployment.



System Prerequisites
====================
 - Traefik v3.6
 - bitnamilegacy/kafka:3.9.0
 - provectuslabs/kafka-ui:latest
 - postgres:16-alpine
 
The docker-compose file in the 'System Prerequisites' folder contains the necessary
packages to install and should you wish to do so you yourself you can run the below -

Install Kafka, Traefik and postgreSql:  docker-compose -f docker-compose.kafka.traefik.postgresql.yml up -d
   
Remove Kafka, Traefik and postgreSql:   docker-compose -f docker-compose.kafka.traefik.postgresql.yml down -v 



Setup Instructions
==================
1. Install Desktop Rancher and pgAdmin 4 to use for postgreSql
2. Download source code from Github
3. In C# ensure the 'fraud_poc_project' is set as 'Start up'
   otherwise you will get an ERROR:
   "A project with an Output Type of Class Library cannot be started directly."

4. Run C# solution locally to ensure it can connect and do local testing
5. Included is the .\deploy-https.ps1 script should you wish to install the latest docker image to Rancher Desktop,
   instead of using the github workflow in the github Actions.
   
   This script is for assistance and in no way a requirement, if you use it then when
   asked for aws credentials run 'aws configure' and supply the credentials for the 
   aws-sbx-general -> 060440565535-sbxgen-c-dev-role
   
   the region        = af-south-1
   the output format = json
   
   NOTE: .\deploy-https.ps1 only installs the docker image from Docker Hub to Rancher Desktop
         It **DOES NOT** upload the latest docker image from your PC to Docker Hub.

         Uploading the docker image to docker hub is done when merging the code into the 'develop' branch in github.
         
         If you have made code changes but not uploaded it to Docker Hub then this script
         will just pull the old code (last updated Docker Image) from Docker Hub
   
6. Once the docker image has been promoted to Rancher Desktop or you have the solution running locally you can refer to
   the 'Testing_deployed_App.md' as guide to test the running application.
   
7. For the Fraud Login endpoint the
   Username = 'fraud-analyst'
   Password = 'SecurePass123!'
   
   Press 'Authorize' and paste generated token
   
8.When running Unit Test in C# ensure PostgreSQL and Kafka is running   


Test Metrics
============
1. login in to obtain token apply
2. take snapshot
3. load simulator
4. check stats

