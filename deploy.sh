#!/bin/bash
echo "Publishing POMS..."
dotnet publish POMS.Web/POMS.Web.csproj -c Release -o ./publish

echo "Deploying to /var/www/pams safely..."
# Use rsync to copy files, deleting old ones EXCEPT App_Data and wwwroot/uploads
sudo rsync -av --delete --exclude='App_Data' --exclude='wwwroot/uploads' ./publish/ /var/www/pams/

echo "Restarting service..."
sudo systemctl restart pams
echo "Deployment complete!"
