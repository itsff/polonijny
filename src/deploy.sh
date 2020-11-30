#!/bin/bash

sudo docker build . -t polonijny:latest
sudo docker save polonijny:latest > /tmp/polonijny.tar
scp /tmp/polonijny.tar polonijny:/tmp/polonijny.tar
ssh polonijny "docker load -i /tmp/polonijny.tar"
echo Done
