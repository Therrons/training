- exit out of docker container shell command line: exit


- to start docker container: docker start <container_id>

- to see all docker containers: docker ps -a
- to see all docker images: docker images

- ======= remove docker container and image commands =======	
- to stop docker container: docker stop <container_id>
- to remove docker container: docker rm <container_id>
- to remove docker image: docker rmi <image_id>
- ==========================================================

- docker run --rm <your-image> printenv | grep ASPNETCORE

- to build docker image: docker build -t <image_name> .

- to run docker container: docker run -p <host_port>:<container_port> <image_name>
- to run docker container in detached mode: docker run -d -p <host_port>:<container_port> <image_name>

- to access docker container shell: docker exec -it <container_id> /bin/bash

- to copy files from host to docker container: docker cp <host_file_path> <container_id>:<container_file_path>
- to copy files from docker container to host: docker cp <container_id>:<container_file_path> <host_file_path>

- to view docker container environment variables: docker exec <container_id> printenv
- to view docker container environment variables: docker exec <container_id> env

- to view docker container port mappings: docker port <container_id>
- to view docker container volumes: docker inspect <container_id> | grep "Mounts"
- to view docker container health status: docker inspect <container_id> | grep "Health"
- to view docker container uptime: docker inspect <container_id> | grep "StartedAt"
- to view docker container restart policy: docker inspect <container_id> | grep "RestartPolicy"

- to view docker container resource usage: docker stats <container_id>
- to view docker container resource limits: docker inspect <container_id> | grep "Resources"
- to view docker container labels: docker inspect <container_id> | grep "Labels"

- to view docker container stats in real-time: docker stats <container_id>
- to view docker container processes: docker top <container_id>
- to view docker container network connections: docker exec <container_id> netstat -tuln
- to view docker container disk usage: docker exec <container_id> df -h
- to view docker container memory usage: docker exec <container_id> free -m
- to view docker container CPU usage: docker exec <container_id> top -b -n 1 | grep "Cpu(s)"

- to view docker container processes: docker exec <container_id> ps aux

- to view docker container network settings: docker inspect <container_id> | grep "IPAddress"
- to view docker container network interfaces: docker exec <container_id> ip addr

- to view docker container file system: docker exec <container_id> ls -l /

- to view docker container logs in real-time: docker logs -f <container_id>
- to view docker container logs: docker logs <container_id>
- to view docker container logs with timestamps: docker logs --timestamps <container_id>
- to view docker container logs with tail: docker logs --tail <number_of_lines> <container_id>
- to view docker container logs with since: docker logs --since <timestamp> <container_id>
- to view docker container logs with until: docker logs --until <timestamp> <container_id>
- to view docker container logs with follow: docker logs --follow <container_id>
- to view docker container logs with details: docker logs --details <container_id>
- to view docker container logs with tail and follow: docker logs --tail <number_of_lines> --follow <container_id>
- to view docker container logs with since and follow: docker logs --since <timestamp> --follow <container_id>
- to view docker container logs with until and follow: docker logs --until <timestamp> --follow <container_id>
- to view docker container logs with details and follow: docker logs --details --follow <container_id>
- to view docker container logs with timestamps and follow: docker logs --timestamps --follow <container_id>
- to view docker container logs with tail, timestamps and follow: docker logs --tail <number_of_lines> --timestamps --follow <container_id>
- to view docker container logs with since, timestamps and follow: docker logs --since <timestamp> --timestamps --follow <container_id>
- to view docker container logs with until, timestamps and follow: docker logs --until <timestamp> --timestamps --follow <container_id>
- to view docker container logs with details, timestamps and follow: docker logs --details --timestamps --follow <container_id>
- to view docker container logs with tail, details and follow: docker logs --tail <number_of_lines> --details --follow <container_id>
- to view docker container logs with since, details and follow: docker logs --since <timestamp> --details --follow <container_id>
-