FROM ubuntu:18.04

MAINTAINER Filip Fracz <filip@basically.me>

ENV DEBIAN_FRONTEND noninteractive

RUN apt-get update
RUN apt-get -y install sed python3-pip python3-dev language-pack-pl libffi-dev libssl-dev

ADD . /app
WORKDIR /app

RUN pip install -r requirements.txt
EXPOSE 80

CMD ["gunicorn", "-w", "4", "-b", "0.0.0.0:80", "app:app"]
