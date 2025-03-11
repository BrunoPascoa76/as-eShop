#!/bin/bash

while true; do
  ITEM_ID=$((RANDOM % 103 + 1))
  URL="https://localhost:7298/item/$ITEM_ID"
  
  # Send POST request with empty JSON body
  curl -k -X POST -H "Content-Type: application/x-www-form-urlencoded" -d "" "$URL"
  
  echo "Sent POST to $URL"
  sleep 1  # Adjust delay betw
done