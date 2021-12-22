#!/bin/bash

help() {
    echo "-u YouTube URL"
    echo "-m Music name"
    echo "-r Artist name"
    echo "-l Album name"
}

url=""
artist=""
album=""
name=""
while [[ "$#" -gt 0 ]]; do
    case $1 in
        -u|--url) url="$2"; shift ;;
        -m|--music) name="$2"; shift ;;
        -r|--artist) artist="$2"; shift ;;
        -l|--album) album="$2"; shift ;;
        *) help; exit 1 ;;
    esac
    shift
done

if [ -z "$url" -o -z "$name" -o -z "$artist" -o -z "$album"  ]; then
    help
    exit 1
fi

mkdir -i tmp
youtube-dl $url -o "tmp/$name"
ffmpeg -i "`ls "tmp/$name".*`" "data/$name.wav"
rm `ls "tmp/$name".*`