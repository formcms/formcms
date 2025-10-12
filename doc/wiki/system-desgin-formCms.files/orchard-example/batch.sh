URL="https://localhost:5001/seed"

# Loop from 3000 to 999000 in steps of 1000 (adjust step if needed)
for ((i=3000; i<=999000; i+=1000)); do
  echo "Calling $URL?start=$i"
  curl -k "$URL?start=$i"
  echo -e "\n---"
done