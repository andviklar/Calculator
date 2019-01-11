FROM microsoft/dotnet:2.1-sdk
WORKDIR /app


COPY *.csproj ./
RUN dotnet restore 

# copy and build everything else
COPY . ./
RUN dotnet publish -c Release -o out
ENTRYPOINT ["dotnet", "out/CalculatorConsole.dll"]