#!/bin/bash

help() {
    echo "-u YouTube URL"
    echo "-n Music name"
    echo "-a Artist name"
}

url=""
artist=""
name=""
while [[ "$#" -gt 0 ]]; do
    case $1 in
        -u|--url) url="$2"; shift ;;
        -n|--name) name="$2"; shift ;;
        -a|--artist) artist="$2"; shift ;;
        *) help; exit 1 ;;
    esac
    shift
done

if [ -z "$url" -o -z "$name" -o "$artist"  ]; then
    help
    exit 1
fi