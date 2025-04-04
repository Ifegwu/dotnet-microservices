#!/bin/bash

# Clean existing certificates
dotnet dev-certs https --clean

# Generate new certificate
dotnet dev-certs https -v -ep ~/.aspnet/https/aspnetapp.pfx -p "Success1ndnture123"

# Convert to .crt
openssl pkcs12 -in ~/.aspnet/https/aspnetapp.pfx -clcerts -nokeys -out aspnetapp.crt -passin pass:SecurePassword123

# System trust
sudo cp aspnetapp.crt /usr/local/share/ca-certificates/
sudo update-ca-certificates

# Firefox trust
certutil -d sql:$HOME/.pki/nssdb -A -t "C,," -n "ASP.NET Core HTTPS" -i aspnetapp.crt

echo "Certificate trust configured!"