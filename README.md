# Platform service 

The platform service is the parent for the command service, the command srv would not exist outside the platform w/o the platform service

```dotnet new webapi -n PlatformService```

## Docker build image

```docker built -t dockerhubid/platformservice .```
```docker push hubid/platformservice ```
To run the image 
```docker run -p 8080:80 -d dockerhubid/platformservice``` --> note also in the K8s deployment.yaml file need to mention the dockerhub id and our docker img name(under image). \

Similarly i ve made the docker image for the command service as well and will be deployed in K8s. \

```docker ps``` 
```docker start/stop containerId```
```docker push hubid/platformservice ```

## K8s 

The platform service and the command service are running inside the K8s clusters and they communicate thru the clusterIP, the entry pt or the gateway for our cluster will be provided by node port service

```kubectl apply -f platforms.deply.yaml``` --> will create the dployment
```kubectl get deployments``` ---> to see all the deployments
```kubectl get pods```
```kubectl delete deployments platforms-depl```
```kubectl apply -f platforms-depl.yaml``` --> to apply any changes that are made after the deployments such as increase the replica
and similarly for the node port depl.yml as well (except the entrypoint this time is commandservice.dll instead of platformservice.dll)

```kubectl rollout restart deployments plantforms.depl``` --> to forcefully restart our pod (once the changes made)

```kubectl get services``` --> fetch all the avalaible services.

## Command Service 

The routes for command srv has to contain the valid resource id of a valid platform, we can't create a command w/o the platform hence we ve to provide a resource id as well. \
same rule in order to retrive or update we ve to use the ref to the platform id 

```dotnet new webapi -n CommandService ```

This service should listen to the port 6000 for the testing purpose not in conflict with the platform service.

This service is gon to ve the platformservice controller and commandservice controller. the way the i will architect is basically the platform resource will act as a parent source.

## Ingress Nginx controller

The Ingress Nginx is containver pod will act as a loadbalancer/ Gateway
```kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.5.1/deploy/static/provider/cloud/deploy.yaml```

- since this pod is completely diff namespace than ours(2).
- ```kubectl get namespace``` --> now we can see all the available pods 

to specifically get the nginx pod 
``` kubectl get pods --namespace=ingress-nginx```
```kubectl get services --namespace=ingress-nginx``` --> to see the services running in nginx (by default its a LB and cluster ip services).

- And then ve to create a ingress-srv.yaml file which specifies the path : /api/platforms for the platform cluster ip service and path : /api/c/plaforms for the command cluster ip service.
- then add the acme.com (the gateway)to the host file ---> 127.0.0.1 acme.com
- finally need to apply the changes ```kubectl apply -f ingress-srv.yaml```
- now instead of putting the http://localhost:3047/api/platforms/ we can use http://acme.com/api/platforms/ it will route to our platform service.

## sql server pod

this sql server pod is for the platforms service (along with the cluster ip srv), this will also have the persistent volume claim (pvc) for the storage
```kubectl get storageclass```

local-pvc.yaml --> to create the pvc on our local storage --> ```kubectl apply -f local-pvc.yaml``` once its create we can see that 
```kubectl get pvc``` 
- Then ve to create a secret file for our sql admin
- ```kubectl create secret generic mssql --from-literal=SA_PASSWORD="password"```
- This secret will be injected as env var in the sql-plat-depl.yaml file, and this sql pod will ve our pvc, this sql yml ve the cluster ip service and the LB service (kinda like NP service which will allow us to directly access to this sql)

```kubectl apply -f mssql-plat-depl.yaml```
and add this connection string to the appsettings.production.json

# Migrations

To generate the migrations 
``` dotnet ef migrations add initialmigrations ``` --> will tell the sql svr how to create the table and so

## Messaging service

The whole point of this build is, when someone create a platform in the platform srv we wanna notify to the command srv that  the new platform is being created and add that to the command service. \

We wanna achieve that by the event driven arch to do that.

## RabbitMQ Message Bus

- The RabbitMq is gon be the message broker as a part of our message bus, the rabbitmq uses AMQP advance message queing protocol .\

- So when we create a platform on the platform srv it will publish to the message bus and then in the command srv we re gon subscribe for those events. \
- The rabbitmq has 4 types of exchange 1. direct 2. fanout 3.topic 4. Header. \
- It delivers msgs to all queues that are bound to exchange, it ignres the routing key, and its ideal for the broadcasting msgs 

Create a rabbitmq-depl.yaml --> will ve cluster ip and LB srv ```kubectl apply -f rabbitmq-depl.yaml```

this has 2 ports one for the mgmt and the other for msg. \

Note : similar to the platform and the command we ve to create a separate DTO for the rabbitmq as well, so in the platform service we create platform pub DTO, and in the command srv, we ve to create command SUB DTO.

# service lifetime

when we register the services they ve diff lifetimes

- Singleton : created 1st time requested, subsequent req use the same instance
- Scoped : same within the req, but created for every new req
- Transient : New instance provided every time, never the same/ reused.

So when we wana create the listening service that'll be singleton, so its gon be there for the lifetime of our app. \

## gRPC 
As per our solution arch, this gRPC connc is the third and the final as we ve already established the http, async pub sub. In our scenario the command srv is the client and it reaches out the platform srv which is considered as the gRPC.\
Just add the port numb and the name in the platforms-depl.yaml --> will just change the cluster ip config state

Google Remote Procedure Call,
- Uses http/2 protocol to transport binary msgs 
- High performance
- Relies on protocol buffer protbuff to define contract b/w the endpts 
- frequently used as method of service to service communication

Add the protos file in both platform and command service
Then finally add the grpc endpt in appsetings.json for both dev and prod


