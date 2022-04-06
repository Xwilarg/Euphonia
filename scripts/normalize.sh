#!/bin/bash

source ~/.profile
set -e
ffmpeg-normalize ../data/raw/*.wav -pr -ext wav -of ../data/normalized