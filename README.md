# SSVEP BCI with Unicorn Hybrid Black (Unity)

## Overview
Unity-based SSVEP experiment system using Unicorn Hybrid Black EEG.

## Features
- UDP EEG reception from Unicorn Recorder
- UDP trigger transmission
- SSVEP experiment controller
- Event logging

## Requirements
- Unity (version)
- Unicorn Recorder
- Unicorn Hybrid Black

## Network Settings
- Trigger Input: 127.0.0.1:1000
- Data Output: 127.0.0.1:1001

## How to Run
1. Start Unicorn Recorder
2. Enable UDP input/output
3. Run Unity scene
4. Execute experiment

## Data Output
- raw EEG: CSV
- event log: CSV

## Future Work
- CCA / FBCCA integration
- Real-time classification