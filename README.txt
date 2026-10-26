Setup Instructions
==================

1. Install Desktop Rancher and pgAdmin 4 to use for postgreSql
2. Download source code from Github
3. In C# ensure the 'fraud_poc_project' is set as 'Start up'
4. Run the following script with docker command to install Traefik and postgreSql
   docker-compose -f docker-compose.traefik.postgresql.yml up -d
5. Check the 'Testing_deployed_App.docx' for the urls for the
   postgreSql and Traefik to ensure they have a user interface
6. Run C# solution locally to ensure it can connect and do local testing
7. Run .\deploy-https.ps1 as ADMINISTRATOR in powershell to install the C# solution into Rancher Desktop
   if asked for aws credentials then run 'aws configure' and supply the credentials for the 
   aws-sbx-general -> 060440565535-sbxgen-c-dev-role
   
   the region        = af-south-1
   the output format = json
8. When the C# app is installed in Rancher Desktop run 'https://localhost/swagger' in your browser
9. For the Fraud Login endpoint the
   Username = 'fraud-analyst'
   Password = 'SecurePass123!'
   
   Press 'Authorize' and paste generated token
   



Test Metrics
============
1. login in to obtain token apply
2. take snapshot
3. load simulator
4. check stats

