#!/bin/bash

set -e
ffmpeg-normalize ../data/raw/*.wav -pr -ext wav -of ../data/normalized