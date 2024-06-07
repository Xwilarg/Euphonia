#!/bin/bash

source ~/.profile
set -e
ffmpeg-normalize ../web/data/raw/*.mp3 -pr -ext mp3 -of ../web/data/normalized -c:a libmp3lame