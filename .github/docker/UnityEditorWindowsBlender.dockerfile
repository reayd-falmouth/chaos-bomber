# game-ci/unity-builder runs Unity inside this image on windows-latest. Blender must be in the image, not on the runner host.
ARG UNITY_IMAGE=unityci/editor:windows-6000.3.9f1-windows-il2cpp-3
FROM ${UNITY_IMAGE}
SHELL ["powershell.exe", "-Command"]
RUN choco install blender -y --no-progress
