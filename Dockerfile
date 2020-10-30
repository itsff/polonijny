#
# Usage:
# * docker build . -f Dockerfile -t polonijny:latest
# * docker run -it -e MONGO_URI=xxxx -p 5005:80 polonijny:latest
# * docker save polonijny:latest > polonijny.tar
# * docker load -i polonijny.tar -t polonijny:latest
#


FROM python:3.8-slim AS compile-image
ENV DEBIAN_FRONTEND="noninteractive"

RUN apt-get update \
    && apt-get -y upgrade \
    && apt-get update \
    && apt-get install -y apt-utils \
    && apt-get install -y g++ libffi-dev libssl-dev python3-wheel \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

RUN python -m venv /opt/venv

# Make sure we use the virtualenv:
ENV PATH="/opt/venv/bin:$PATH"
RUN pip3 install --upgrade pip

COPY ./requirements.txt .
RUN pip3 install -r requirements.txt --use-feature=2020-resolver


### Final image --------------------------------------------------------------------------------------
FROM python:3.8-slim AS build-image
RUN apt-get update \
    && apt-get -y upgrade \
    && apt-get update \
    && apt-get install --reinstall -y locales \
    && apt-get autoremove \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Configure Polish locale
RUN sed -i 's/# pl_PL.UTF-8 UTF-8/pl_PL.UTF-8 UTF-8/' /etc/locale.gen && locale-gen pl_PL.UTF-8
ENV LANG pl_PL.UTF-8
ENV LANGUAGE pl_PL
ENV LC_ALL pl_PL.UTF-8
RUN dpkg-reconfigure --frontend noninteractive locales

COPY --from=compile-image /opt/venv /opt/venv
COPY *.py /app/
COPY static /app/static
COPY templates /app/templates

# Make sure we use the virtualenv:
ENV PATH="/opt/venv/bin:$PATH"
WORKDIR /app
CMD ["gunicorn", "-w", "1", "-b", "0.0.0.0:80", "app:app"]
