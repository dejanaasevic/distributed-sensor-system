kubectl delete -f k8s/ingress.yaml,k8s/sensor.yaml,k8s/consensus.yaml,k8s/ingestion.yaml,k8s/notification.yaml,k8s/db.yaml,k8s/secrets.yaml

kubectl apply -f k8s/secrets.yaml && \
kubectl apply -f k8s/db.yaml && \
kubectl apply -f k8s/notification.yaml && \
kubectl apply -f k8s/ingestion.yaml && \
kubectl apply -f k8s/consensus.yaml && \
kubectl apply -f k8s/sensor.yaml && \
kubectl apply -f k8s/ingress.yaml