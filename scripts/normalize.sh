#!/bin/bash

source ~/.profile
set -e
ffmpeg-normalize ../data/raw/*.mp3 -pr -ext mp3 -of ../data/normalized -c:a libmp3lame