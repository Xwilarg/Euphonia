#!/bin/bash

set -e
ffmpeg-normalize ../data/*.wav -pr -ext wav -of ../data/normalized
rm -rf ../data/*.wav