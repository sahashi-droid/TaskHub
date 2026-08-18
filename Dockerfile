# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# TODO
# Create a stage named "restore" based on the .NET 8 SDK image.
# In that stage:
# 1. Set the working directory to /src
# 2. Copy only TaskHub.csproj
# 3. Run "dotnet restore" for TaskHub.csproj

# TODO
# Create a stage named "build" that starts FROM the "restore" stage.
# In that stage:
# 1. Copy the rest of the project files
# 2. Run "dotnet build" in Release mode
# 3. Use --no-restore because restore was already done earlier

FROM build AS migrations
RUN dotnet tool install --global dotnet-ef --version 8.0.13
ENV PATH="${PATH}:/root/.dotnet/tools"
ENTRYPOINT ["dotnet", "ef", "database", "update", "--project", "TaskHub.csproj", "--startup-project", "TaskHub.csproj", "--configuration", "Release", "--no-build"]

# TODO
# Create a stage named "publish" that starts FROM the "build" stage.
# In that stage:
# 1. Run "dotnet publish" in Release mode
# 2. Publish the output to /app/publish
# 3. Use --no-build because the project was already built earlier
# 4. Disable the app host by using /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER app
ENTRYPOINT ["dotnet", "TaskHub.dll"]
