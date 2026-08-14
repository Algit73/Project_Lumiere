"""
generate_filler_audio.py

Offline helper (NOT part of the Unity runtime) that synthesizes the filler-line audio
clips used by FillerLineBankSO via the OpenAI Text-to-Speech API.

Workflow:
    1. In Unity: Tools > Lumiere > Export Filler Texts To JSON
       (writes Assets/Scripts/OpenAI/Tools/filler_lines.json)
    2. Run this script from anywhere:  python generate_filler_audio.py
       (reads the same OPENAI_KEY.txt already used by OpenAIBasics.cs)
    3. Back in Unity: Tools > Lumiere > Auto-Assign Filler Audio Clips
       (matches the generated files onto FillerLineBankSO by index)

Output:
    Assets/Audio/Fillers/Wrong/wrong_00.mp3, wrong_01.mp3, ...
    Assets/Audio/Fillers/Hint/hint_00.mp3, hint_01.mp3, ...

Requires: pip install requests
"""

import json
import os
import sys

import requests

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
# Assets/Scripts/OpenAI/Tools/ -> Assets/
ASSETS_DIR = os.path.abspath(os.path.join(SCRIPT_DIR, "..", "..", ".."))

KEY_PATH = os.path.join(ASSETS_DIR, "Scripts", "OpenAI", "OPENAI_KEY.txt")
JSON_PATH = os.path.join(SCRIPT_DIR, "filler_lines.json")

WRONG_OUT_DIR = os.path.join(ASSETS_DIR, "Audio", "Fillers", "Wrong")
HINT_OUT_DIR = os.path.join(ASSETS_DIR, "Audio", "Fillers", "Hint")

TTS_MODEL = "tts-1"
TTS_VOICE = "alloy"
TTS_URL = "https://api.openai.com/v1/audio/speech"


def load_api_key() -> str:
    with open(KEY_PATH, "r", encoding="utf-8") as f:
        return f.read().strip()


def load_filler_lines() -> dict:
    if not os.path.exists(JSON_PATH):
        sys.exit(
            f"Missing {JSON_PATH}.\n"
            "Run Tools > Lumiere > Export Filler Texts To JSON in Unity first."
        )
    with open(JSON_PATH, "r", encoding="utf-8") as f:
        return json.load(f)


def synthesize(api_key: str, text: str, out_path: str) -> None:
    response = requests.post(
        TTS_URL,
        headers={
            "Authorization": f"Bearer {api_key}",
            "Content-Type": "application/json",
        },
        json={
            "model": TTS_MODEL,
            "voice": TTS_VOICE,
            "input": text,
            "response_format": "mp3",
        },
        timeout=60,
    )
    response.raise_for_status()
    with open(out_path, "wb") as f:
        f.write(response.content)


def generate_set(api_key: str, texts: list, out_dir: str, prefix: str) -> None:
    os.makedirs(out_dir, exist_ok=True)
    for i, text in enumerate(texts):
        filename = f"{prefix}{i:02d}.mp3"
        out_path = os.path.join(out_dir, filename)
        print(f"  [{prefix}{i:02d}] \"{text}\" -> {out_path}")
        synthesize(api_key, text, out_path)


def main() -> None:
    api_key = load_api_key()
    lines = load_filler_lines()

    print("Generating wrong-guess fillers...")
    generate_set(api_key, lines.get("wrong", []), WRONG_OUT_DIR, "wrong_")

    print("Generating hint fillers...")
    generate_set(api_key, lines.get("hint", []), HINT_OUT_DIR, "hint_")

    print("\nDone. In Unity: Tools > Lumiere > Auto-Assign Filler Audio Clips")


if __name__ == "__main__":
    main()
