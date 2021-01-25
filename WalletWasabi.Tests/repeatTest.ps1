dotnet build

for ($i = 0; $i -lt 10; $i++) {

  Write-Host "[#$($i)] Started ..."
  dotnet test --no-restore --filter "SelectMostPrivateIndependentlyOfCluster" >"temp/result.$i.log" 2>&1

  if ($?) {
    Write-Host "[#$($i)] Test succeeded!"
  } else {
    Write-Host "[#$($i)] Test failed!"
  }
}
