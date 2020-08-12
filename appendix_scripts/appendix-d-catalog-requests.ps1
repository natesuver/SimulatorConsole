#Appendix D: The powershell script used to compute cache hit ratio

Param 
(
    [string] $traceDirectory,
    [string] $procedurename
) 
$nodeCount = 0
$requests = @()
$cacheHits = 0
Get-ChildItem $traceDirectory\* -Include *.bece, *.rml | 
Foreach-Object {
    $xml = ([xml](Get-Content -Path $_.FullName))
    $procedureNodes = $xml.SelectNodes("//CMD[contains(text(), '$procedurename')]")
    $nodeCount+=$procedureNodes.Count
    foreach ($procedureCommand in $procedureNodes) {
        $parentText = $procedureCommand.ParentNode.InnerText
        if ($requests -contains $parentText) {
            $cacheHits++;
        }
        if ($requests -notcontains $parentText) {
            $requests+=$parentText;
        }
    }
}
write-host "Cache Hits: $cacheHits |  Total Requests: $nodeCount"

#Sample Usage
#. .\catalog-requests.ps1 -traceDirectory "C:\trace" -procedurename "<your procedure name>"
