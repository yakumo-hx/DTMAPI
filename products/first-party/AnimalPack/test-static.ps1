[CmdletBinding()]
param(
    [string]$PackageRoot
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version 2.0
Add-Type -AssemblyName System.Drawing

$productRoot = [IO.Path]::GetFullPath($PSScriptRoot)
$repoRoot = [IO.Path]::GetFullPath((Join-Path $productRoot '..\..\..'))
$isPackage = -not [string]::IsNullOrWhiteSpace($PackageRoot)
$dataRoot = if ($isPackage) { [IO.Path]::GetFullPath($PackageRoot) } else { $productRoot }

function Read-Json([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Missing JSON file: $Path"
    }
    try {
        $parsed = Get-Content -LiteralPath $Path -Raw -Encoding UTF8 | ConvertFrom-Json
        if ($parsed -is [System.Array]) {
            foreach ($entry in $parsed) {
                Write-Output $entry
            }
            return
        }
        return $parsed
    }
    catch {
        throw "Invalid JSON '$Path': $($_.Exception.Message)"
    }
}

function Assert-True([bool]$Condition, [string]$Message) {
    if (-not $Condition) {
        throw "ASSERTION FAILED: $Message"
    }
}

function Assert-Equal($Actual, $Expected, [string]$Message) {
    if ([string]$Actual -cne [string]$Expected) {
        throw "ASSERTION FAILED: $Message. Expected '$Expected'; actual '$Actual'."
    }
}

function New-IdMap($Rows, [string]$Label) {
    $map = @{}
    foreach ($row in @($Rows)) {
        $id = [string]$row.id
        Assert-True (-not [string]::IsNullOrWhiteSpace($id)) "$Label contains an empty id"
        Assert-True (-not $map.ContainsKey($id)) "$Label contains duplicate id '$id'"
        $map[$id] = $row
    }
    return $map
}

function Assert-ExactSet([object[]]$Actual, [object[]]$Expected, [string]$Message) {
    $actualText = @($Actual | ForEach-Object { [string]$_ } | Sort-Object) -join '|'
    $expectedText = @($Expected | ForEach-Object { [string]$_ } | Sort-Object) -join '|'
    Assert-Equal $actualText $expectedText $Message
}

function ConvertFrom-UnicodeCodePoints([int[]]$CodePoints) {
    return -join @($CodePoints | ForEach-Object { [char]$_ })
}

function Get-TextSha256([string]$Text) {
    $sha256 = [Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [Text.Encoding]::UTF8.GetBytes($Text)
        return ([BitConverter]::ToString($sha256.ComputeHash($bytes))).Replace('-', '')
    }
    finally {
        $sha256.Dispose()
    }
}

function Get-PngGeometry([string]$Path) {
    $bitmap = [System.Drawing.Bitmap]::new($Path)
    try {
        $minX = $bitmap.Width
        $minY = $bitmap.Height
        $maxX = -1
        $maxY = -1
        for ($y = 0; $y -lt $bitmap.Height; $y++) {
            for ($x = 0; $x -lt $bitmap.Width; $x++) {
                if ($bitmap.GetPixel($x, $y).A -gt 0) {
                    if ($x -lt $minX) { $minX = $x }
                    if ($x -gt $maxX) { $maxX = $x }
                    if ($y -lt $minY) { $minY = $y }
                    if ($y -gt $maxY) { $maxY = $y }
                }
            }
        }
        if ($maxX -lt $minX -or $maxY -lt $minY) {
            throw "PNG contains no visible pixels: $Path"
        }
        return [pscustomobject]@{
            CanvasWidth = $bitmap.Width
            CanvasHeight = $bitmap.Height
            VisibleWidth = $maxX - $minX + 1
            VisibleHeight = $maxY - $minY + 1
            LeftPadding = $minX
            RightPadding = $bitmap.Width - $maxX - 1
            TopPadding = $minY
            BottomPadding = $bitmap.Height - $maxY - 1
        }
    }
    finally {
        $bitmap.Dispose()
    }
}

function Get-ImageMetadata([string]$Path) {
    $image = [System.Drawing.Image]::FromFile($Path)
    try {
        return [pscustomobject]@{
            Width = $image.Width
            Height = $image.Height
            IsPng = $image.RawFormat.Guid -eq [System.Drawing.Imaging.ImageFormat]::Png.Guid
        }
    }
    finally {
        $image.Dispose()
    }
}

if (-not (Test-Path -LiteralPath $dataRoot -PathType Container)) {
    throw "AnimalPack root does not exist: $dataRoot"
}

foreach ($jsonFile in @(Get-ChildItem -LiteralPath $dataRoot -Recurse -File -Filter '*.json')) {
    $null = Read-Json $jsonFile.FullName
}

$contentRoot = Join-Path $dataRoot 'Content'
$manifestPath = if ($isPackage) { Join-Path $contentRoot 'DTMAPI\manifest.json' } else { Join-Path $dataRoot 'manifest.json' }
$infoPath = if ($isPackage) { Join-Path $dataRoot 'info.json' } else { Join-Path $dataRoot 'official-info.json' }
$manifest = Read-Json $manifestPath
$officialInfo = Read-Json $infoPath
$assetConfig = Read-Json (Join-Path $productRoot 'asset-sources.json')
Assert-Equal $assetConfig.schemaVersion 3 'asset-source schema version'
Assert-Equal $manifest.UniqueID 'DTMAPI.AnimalPack' 'manifest UniqueID'
Assert-Equal $manifest.Name (ConvertFrom-UnicodeCodePoints @(0x7F3A, 0x6C27, 0x52A8, 0x7269, 0x5305)) 'manifest display name'
Assert-Equal $manifest.Author 'Yuuka' 'manifest author'
Assert-Equal $manifest.Version '1.0.0' 'manifest version'
Assert-Equal $manifest.Type 'ContentPack' 'manifest type'
Assert-True ($null -eq $manifest.PSObject.Properties['EntryDll']) 'content-pack manifest must omit EntryDll'
Assert-Equal $manifest.MinimumDTMApiVersion '0.6.0' 'manifest minimum DTMAPI version'
Assert-True ($null -eq $manifest.PSObject.Properties['Dependencies']) 'content-pack manifest must omit Dependencies'
Assert-True (-not (Test-Path -LiteralPath (Join-Path $contentRoot 'DTMAPI\dtmapi-package.json'))) 'legacy dtmapi-package.json must be absent'
Assert-Equal $officialInfo.name (ConvertFrom-UnicodeCodePoints @(0x7F3A, 0x6C27, 0x52A8, 0x7269, 0x5305)) 'official-info display name'
Assert-Equal $officialInfo.author 'Yuuka' 'official-info author'
Assert-Equal $officialInfo.localized_name.schinese $officialInfo.name 'official-info Simplified Chinese name'
Assert-Equal $manifest.Description $officialInfo.description 'manifest and official-info descriptions'
Assert-Equal $officialInfo.localized_description.schinese $officialInfo.description 'official-info Simplified Chinese description'
Assert-ExactSet @($officialInfo.localized_description.PSObject.Properties.Name) @('schinese', 'tchinese', 'english') 'official-info localized-description languages'
Assert-Equal (Get-TextSha256 ([string]$officialInfo.description)) '125ACEECE2667F184A8957BF757EAFD5EEC999153D211A6E7DFF5B068A82A026' 'frozen requested Simplified Chinese description'
Assert-Equal (Get-TextSha256 ([string]$officialInfo.localized_description.tchinese)) 'B2934BD613C59D1F383638CC2C0948AE59C12D734479B0F47735D626B17D1EA7' 'frozen Traditional Chinese Steam description'
Assert-Equal (Get-TextSha256 ([string]$officialInfo.localized_description.english)) 'A139839A9BE782DF1F004EF85DCC93009620D953B1431ACAB0BC4557AC43383D' 'frozen English Steam description'
Assert-Equal @(([string]$officialInfo.description) -split "`n").Count 19 'Simplified Chinese Steam description line count'
Assert-Equal @(([string]$officialInfo.localized_description.tchinese) -split "`n").Count 19 'Traditional Chinese Steam description line count'
Assert-Equal @(([string]$officialInfo.localized_description.english) -split "`n").Count 19 'English Steam description line count'
foreach ($descriptionNeedle in @(
    (ConvertFrom-UnicodeCodePoints @(0x51EF, 0x6D85, 0x5C3C, 0x6728)),
    (ConvertFrom-UnicodeCodePoints @(0x5C0F, 0x52A8, 0x7269, 0x7167, 0x6599, 0x7AD9, 0x56FE, 0x7EB8)),
    'DTMAPI 0.6.0',
    '1000G',
    '1h',
    (ConvertFrom-UnicodeCodePoints @(0x94DC, 0x952D, 0x00D7, 0x0035)),
    (ConvertFrom-UnicodeCodePoints @(0x6728, 0x5934, 0x00D7, 0x0031, 0x0030, 0x0030)),
    'PNG',
    'WAV',
    'JSON'
)) {
    Assert-True ([string]$officialInfo.description).Contains([string]$descriptionNeedle) "official-info description contains '$descriptionNeedle'"
}
foreach ($footerNeedle in @(
    (ConvertFrom-UnicodeCodePoints @(0x672C, 0x006D, 0x006F, 0x0064, 0x4E3A, 0x793A, 0x4F8B, 0x006D, 0x006F, 0x0064)),
    (ConvertFrom-UnicodeCodePoints @(0x57FA, 0x4E8E, 0x0044, 0x0054, 0x004D, 0x0041, 0x0050, 0x0049, 0x7684, 0x52A8, 0x7269, 0x62D3, 0x5C55, 0x80FD, 0x529B)),
    (ConvertFrom-UnicodeCodePoints @(0x4F5C, 0x8005, 0x53EA, 0x9700, 0x8981, 0x914D, 0x7F6E, 0x0050, 0x004E, 0x0047, 0x3001, 0x0057, 0x0041, 0x0056, 0x3001, 0x004A, 0x0053, 0x004F, 0x004E, 0x5373, 0x53EF, 0x5B9E, 0x73B0, 0x6DFB, 0x52A0, 0x517B, 0x6B96, 0x52A8, 0x7269)),
    (ConvertFrom-UnicodeCodePoints @(0x6B22, 0x8FCE, 0x5927, 0x5BB6, 0x4F53, 0x9A8C)),
    (ConvertFrom-UnicodeCodePoints @(0x6709, 0x0062, 0x0075, 0x0067, 0x8BF7, 0x53CD, 0x9988, 0xFF0C, 0x559C, 0x6B22, 0x8BF7, 0x70B9, 0x597D, 0x8BC4))
)) {
    Assert-True ([string]$officialInfo.description).Contains([string]$footerNeedle) "official-info example-mod footer contains '$footerNeedle'"
}

$brandingProperties = @($assetConfig.branding.PSObject.Properties)
Assert-ExactSet @($brandingProperties.Name) @('largePreview', 'icon') 'branding asset roles'
$brandingExpected = @{
    largePreview = [pscustomobject]@{
        Role = 'steam-large-preview'
        Source = (ConvertFrom-UnicodeCodePoints @(0x44, 0x3A, 0x2F, 0x56FE, 0x7247, 0x2F, 0x5C01, 0x9762, 0x56FE, 0x2F, 0x7F3A, 0x6C27, 0x52A8, 0x7269, 0x5305, 0x2E, 0x6A, 0x70, 0x67))
        SourceWidth = 1549
        SourceHeight = 925
        SourceHash = '5D217596C9BBD5F4087ECD53CBDE55A559617064B1378BA38EDB6B449CF9C1E6'
        Destination = 'preview.png'
        OutputWidth = 1549
        OutputHeight = 925
        OutputBytes = 849062
        MaximumBytesExclusive = 1000000
        OutputHash = '743206EE3A57402CA20611F6160A738E82E61292EE31400911D866A099ADC96F'
    }
    icon = [pscustomobject]@{
        Role = 'in-game-cover-and-steam-small-preview'
        Source = (ConvertFrom-UnicodeCodePoints @(0x44, 0x3A, 0x2F, 0x56FE, 0x7247, 0x2F, 0x5C01, 0x9762, 0x56FE, 0x2F, 0x7F3A, 0x6C27, 0x52A8, 0x7269, 0x5305, 0x20, 0x2D, 0x20, 0x526F, 0x672C, 0x2E, 0x6A, 0x70, 0x67))
        SourceWidth = 744
        SourceHeight = 482
        SourceHash = '5D332C54242F0CB69A51A2B233FC47BA5416752E364279600C9A86BCEE7B0F90'
        Destination = 'icon.png'
        OutputWidth = 744
        OutputHeight = 482
        OutputHash = '4FF9E28E95123D37C9C80E7C43C067AF0D27C793673C6D489A1DEEF361845960'
    }
}
foreach ($brandingProperty in $brandingProperties) {
    $key = [string]$brandingProperty.Name
    $branding = $brandingProperty.Value
    $expected = $brandingExpected[$key]
    Assert-Equal $branding.role $expected.Role "$key branding role"
    Assert-Equal $branding.source $expected.Source "$key branding source path"
    Assert-Equal $branding.sourceFormat 'JPEG' "$key branding source format"
    Assert-Equal $branding.sourceWidth $expected.SourceWidth "$key branding source width"
    Assert-Equal $branding.sourceHeight $expected.SourceHeight "$key branding source height"
    Assert-Equal $branding.sha256 $expected.SourceHash "$key branding source SHA-256"
    Assert-Equal $branding.destination $expected.Destination "$key branding destination"
    Assert-Equal $branding.outputFormat 'PNG' "$key branding output format"
    Assert-Equal $branding.outputWidth $expected.OutputWidth "$key branding output width"
    Assert-Equal $branding.outputHeight $expected.OutputHeight "$key branding output height"
    Assert-Equal $branding.outputSha256 $expected.OutputHash "$key branding output SHA-256"

    $brandingPath = Join-Path $dataRoot ([string]$branding.destination)
    Assert-True (Test-Path -LiteralPath $brandingPath -PathType Leaf) "$key branding PNG exists"
    Assert-Equal (Get-FileHash -LiteralPath $brandingPath -Algorithm SHA256).Hash $expected.OutputHash "$key branding PNG hash"
    $brandingMetadata = Get-ImageMetadata $brandingPath
    Assert-True $brandingMetadata.IsPng "$key branding asset is a true PNG"
    Assert-Equal $brandingMetadata.Width $expected.OutputWidth "$key branding PNG width"
    Assert-Equal $brandingMetadata.Height $expected.OutputHeight "$key branding PNG height"
    if ($key -ceq 'largePreview') {
        $brandingBytes = (Get-Item -LiteralPath $brandingPath).Length
        Assert-Equal $branding.outputBytes $expected.OutputBytes "$key branding declared byte length"
        Assert-Equal $branding.maximumBytesExclusive $expected.MaximumBytesExclusive "$key branding maximum byte length"
        Assert-Equal $brandingBytes $expected.OutputBytes "$key branding PNG byte length"
        Assert-True ($brandingBytes -lt [long]$expected.MaximumBytesExclusive) "$key branding PNG stays below the Steam preview limit"
    }
}

$animals = New-IdMap (Read-Json (Join-Path $contentRoot 'animal_tbanimal.json')) 'animal_tbanimal'
$documents = New-IdMap (Read-Json (Join-Path $contentRoot 'animal_tbanimaldocument.json')) 'animal_tbanimaldocument'
$husbandry = New-IdMap (Read-Json (Join-Path $contentRoot 'animal_tbhusbandry.json')) 'animal_tbhusbandry'
$items = New-IdMap (Read-Json (Join-Path $contentRoot 'item_tbitem.json')) 'item_tbitem'
$spawnLuts = New-IdMap (Read-Json (Join-Path $contentRoot 'item_tbitemspawn.json')) 'item_tbitemspawn'
$dismantleRecipes = New-IdMap (Read-Json (Join-Path $contentRoot 'recipe_tbdismantlerecipe.json')) 'recipe_tbdismantlerecipe'
$dismantleGroups = New-IdMap (Read-Json (Join-Path $contentRoot 'recipe_tbdismantlerecipegroup.json')) 'recipe_tbdismantlerecipegroup'
$buildRecipes = New-IdMap (Read-Json (Join-Path $contentRoot 'recipe_tbrecipe.json')) 'recipe_tbrecipe'
$equipment = New-IdMap (Read-Json (Join-Path $contentRoot 'equipment_tbequipment.json')) 'equipment_tbequipment'
$customAnimals = New-IdMap ((Read-Json (Join-Path $contentRoot 'DTMAPI\custom-animals.json')) | ForEach-Object {
    [pscustomobject]@{ id = $_.speciesId; row = $_ }
}) 'custom-animals speciesId'
$audioRows = @(Read-Json (Join-Path $contentRoot 'DTMAPI\audio-replacements.json'))

$speciesIds = @('hatch', 'drecko', 'mole', 'oilfloater')
Assert-ExactSet @($animals.Keys) $speciesIds 'animal species set'
Assert-ExactSet @($documents.Keys) $speciesIds 'document species set'
Assert-ExactSet @($husbandry.Keys) $speciesIds 'husbandry species set'
Assert-ExactSet @($customAnimals.Keys) $speciesIds 'DTMAPI custom-animal species set'
Assert-Equal $audioRows.Count 8 'audio replacement row count'

$speciesSpecs = @(
    [pscustomobject]@{ Id = 'hatch'; Template = 'chicken'; Interval = 3; Space = 2; ProduceLut = 'hatch_produce'; Product = 'hatch_egg'; Hidden = 'petrified_hatch_egg'; HiddenCount = 1; Threshold = 60; Bag = 'sack_hatch'; BagTitle = (ConvertFrom-UnicodeCodePoints @(0x9EBB, 0x888B, 0xFF08, 0x54C8, 0x5947, 0xFF09)); BagBuy = 800; BagBaseSell = 400; ChildSell = 640; AdultSell = 2400 },
    [pscustomobject]@{ Id = 'drecko'; Template = 'goat'; Interval = 9; Space = 3; ProduceLut = 'drecko_produce'; Product = 'drecko_egg'; Hidden = 'wool_grease'; HiddenCount = 3; Threshold = 60; Bag = 'sack_drecko'; BagTitle = (ConvertFrom-UnicodeCodePoints @(0x9EBB, 0x888B, 0xFF08, 0x58C1, 0x864E, 0xFF09)); BagBuy = 3000; BagBaseSell = 1500; ChildSell = 2400; AdultSell = 9000 },
    [pscustomobject]@{ Id = 'mole'; Template = 'marsh_pangolin'; Interval = 3; Space = 4; ProduceLut = 'mole_produce'; Product = 'mole_egg'; Hidden = 'fertile_mole_egg'; HiddenCount = 1; Threshold = 40; Bag = 'sack_mole'; BagTitle = (ConvertFrom-UnicodeCodePoints @(0x9EBB, 0x888B, 0xFF08, 0x7530, 0x9F20, 0xFF09)); BagBuy = 5000; BagBaseSell = 2500; ChildSell = 4000; AdultSell = 15000 },
    [pscustomobject]@{ Id = 'oilfloater'; Template = 'slime'; Interval = 3; Space = 2; ProduceLut = 'oilfloater_produce'; Product = 'oilfloater_egg'; Hidden = 'polymer_oilfloater_egg'; HiddenCount = 1; Threshold = 120; Bag = 'sack_oilfloater'; BagTitle = (ConvertFrom-UnicodeCodePoints @(0x9EBB, 0x888B, 0xFF08, 0x6D6E, 0x6E38, 0x751F, 0x7269, 0xFF09)); BagBuy = 1200; BagBaseSell = 600; ChildSell = 960; AdultSell = 3600 }
)

$productionEquipmentSpecs = @{
    hatch = [pscustomobject]@{ Template = 'chicken'; EquipmentId = 'chicken_nest'; Simplified = (ConvertFrom-UnicodeCodePoints @(0x9E21, 0x7A9D)); Traditional = (ConvertFrom-UnicodeCodePoints @(0x96DE, 0x7AA9)); English = 'Chicken Coop' }
    drecko = [pscustomobject]@{ Template = 'goat'; EquipmentId = 'lint_roller'; Simplified = (ConvertFrom-UnicodeCodePoints @(0x7C98, 0x6BDB, 0x6EDA)); Traditional = (ConvertFrom-UnicodeCodePoints @(0x7C98, 0x6BDB, 0x6EFE)); English = 'Lint Roller' }
    mole = [pscustomobject]@{ Template = 'marsh_pangolin'; EquipmentId = 'milking_machine'; Simplified = (ConvertFrom-UnicodeCodePoints @(0x6324, 0x5976, 0x5668)); Traditional = (ConvertFrom-UnicodeCodePoints @(0x64E0, 0x5976, 0x5668)); English = 'Milking Machine' }
    oilfloater = [pscustomobject]@{ Template = 'slime'; EquipmentId = 'honey_comb'; Simplified = (ConvertFrom-UnicodeCodePoints @(0x8702, 0x7BB1)); Traditional = (ConvertFrom-UnicodeCodePoints @(0x8702, 0x7BB1)); English = 'Hive Box' }
}
$simplifiedEquipmentPrefix = ConvertFrom-UnicodeCodePoints @(0x4EA7, 0x51FA, 0x8BBE, 0x5907, 0x4E3A)
$traditionalEquipmentPrefix = ConvertFrom-UnicodeCodePoints @(0x7522, 0x51FA, 0x8A2D, 0x5099, 0x70BA)

foreach ($spec in $speciesSpecs) {
    $animal = $animals[$spec.Id]
    Assert-Equal $animal.schedule_id $spec.Template "$($spec.Id) schedule template"
    $productionEquipment = $productionEquipmentSpecs[$spec.Id]
    Assert-Equal $animal.schedule_id $productionEquipment.Template "$($spec.Id) production-equipment schedule template"
    Assert-True ([string]$officialInfo.description).Contains("$simplifiedEquipmentPrefix$($productionEquipment.Simplified)") "$($spec.Id) Simplified Chinese description names official production equipment '$($productionEquipment.EquipmentId)'"
    Assert-True ([string]$officialInfo.localized_description.tchinese).Contains("$traditionalEquipmentPrefix$($productionEquipment.Traditional)") "$($spec.Id) Traditional Chinese description names official production equipment '$($productionEquipment.EquipmentId)'"
    Assert-True ([string]$officialInfo.localized_description.english).Contains("production equipment: $($productionEquipment.English)") "$($spec.Id) English description names official production equipment '$($productionEquipment.EquipmentId)'"
    Assert-Equal $animal.metabolism_interval $spec.Interval "$($spec.Id) production interval"
    Assert-Equal $animal.space $spec.Space "$($spec.Id) animal space"
    Assert-Equal $animal.produce_spawn_entry.spawn_lut $spec.ProduceLut "$($spec.Id) produce LUT"
    Assert-Equal $animal.produce_spawn_entry.count_range.min_count 1 "$($spec.Id) minimum ordinary product count"
    Assert-Equal $animal.produce_spawn_entry.count_range.max_count 1 "$($spec.Id) maximum ordinary product count"
    Assert-Equal @($animal.levels).Count 2 "$($spec.Id) child/adult level count"
    Assert-Equal $animal.title.key "animal_title_$($spec.Id)" "$($spec.Id) title key"
    Assert-Equal $animal.defaul_input_name.key "animal_default_input_name_$($spec.Id)" "$($spec.Id) default-name key"
    Assert-Equal $animal.levels[0].description_in_sack.key "animal_child_desc_$($spec.Id)" "$($spec.Id) child sack-label key"
    Assert-Equal $animal.levels[1].description_in_sack.key "animal_adult_desc_$($spec.Id)" "$($spec.Id) adult sack-label key"
    Assert-Equal $animal.levels[0].price $spec.ChildSell "$($spec.Id) child selling value"
    Assert-Equal $animal.levels[1].price $spec.AdultSell "$($spec.Id) adult selling value"
    Assert-Equal $animal.levels[1].price ([int]$spec.BagBuy * 3) "$($spec.Id) adult sale is three times juvenile purchase"
    Assert-True $spawnLuts.ContainsKey($spec.ProduceLut) "$($spec.Id) produce LUT exists"
    $produceRows = @($spawnLuts[$spec.ProduceLut].spawn_datas)
    Assert-Equal $produceRows.Count 1 "$($spec.Id) produce LUT row count"
    Assert-Equal $produceRows[0].item_name $spec.Product "$($spec.Id) ordinary product"
    Assert-Equal $produceRows[0].min_count 1 "$($spec.Id) ordinary LUT min"
    Assert-Equal $produceRows[0].max_count 1 "$($spec.Id) ordinary LUT max"

    $husbandryRows = @($husbandry[$spec.Id].husbandry_datas)
    Assert-Equal $husbandryRows.Count 1 "$($spec.Id) husbandry output count"
    Assert-Equal $husbandryRows[0].output $spec.Hidden "$($spec.Id) hidden output"
    Assert-Equal $husbandryRows[0].output_range.x $spec.HiddenCount "$($spec.Id) hidden output minimum"
    Assert-Equal $husbandryRows[0].output_range.y $spec.HiddenCount "$($spec.Id) hidden output maximum"
    Assert-Equal $husbandryRows[0].threshold $spec.Threshold "$($spec.Id) hidden threshold"
    Assert-Equal @($husbandryRows[0].limited_contributions).Count 0 "$($spec.Id) unrestricted contribution list"

    $custom = $customAnimals[$spec.Id].row
    Assert-Equal $custom.templateSpeciesId $spec.Template "$($spec.Id) DTMAPI template species"
    Assert-Equal $custom.aiTemplate $spec.Template "$($spec.Id) DTMAPI AI template"
    foreach ($obsoleteField in @('movementMultiplier', 'metabolismMultiplier', 'packageItemId', 'shopItemListId')) {
        Assert-True ($null -eq $custom.PSObject.Properties[$obsoleteField]) "$($spec.Id) must omit obsolete field '$obsoleteField'"
    }

    Assert-True $items.ContainsKey($spec.Bag) "$($spec.Id) juvenile purchase bag exists"
    $bag = $items[$spec.Bag]
    Assert-Equal $bag.sub_type 'husbandry_animal' "$($spec.Id) bag subtype"
    Assert-Equal $bag.buying_price $spec.BagBuy "$($spec.Id) juvenile purchase price"
    Assert-Equal $bag.selling_price $spec.BagBaseSell "$($spec.Id) empty/base bag selling price"
    Assert-Equal $bag.title.key "item_$($spec.Bag)" "$($spec.Id) bag title key"
    Assert-Equal $bag.title.text $spec.BagTitle "$($spec.Id) bag title"
    Assert-Equal $bag.ui_sprite_asset.url 'icon_item_sack_full' "$($spec.Id) generic animal-bag catalog sprite"
    Assert-Equal $bag.function.'$type' 'ItemFunctionAnimalPackage' "$($spec.Id) bag native function"
    Assert-Equal $bag.function.ui_sprite_catch.url 'icon_item_sack_full' "$($spec.Id) filled animal-bag sprite"
    Assert-Equal $bag.function.preset_animal $spec.Id "$($spec.Id) bag preset species"
    Assert-Equal $bag.function.reusable $false "$($spec.Id) purchased juvenile bag is consumed on release"
    Assert-Equal $bag.function.type_when_full 'husbandry_animal' "$($spec.Id) full bag subtype"
}

$expectedItemIds = @(
    'sack_hatch', 'sack_drecko', 'sack_mole', 'sack_oilfloater',
    'hatch_egg', 'petrified_hatch_egg', 'drecko_egg', 'mole_egg',
    'fertile_mole_egg', 'oilfloater_egg', 'polymer_oilfloater_egg',
    'recipe_animal_care_station', 'animal_care_station'
)
Assert-ExactSet @($items.Keys) $expectedItemIds 'custom item set'

$stationBlueprint = $items['recipe_animal_care_station']
Assert-Equal $stationBlueprint.sub_type 'special_other' 'animal care station blueprint subtype'
Assert-Equal $stationBlueprint.consumable $true 'animal care station blueprint is consumed on use'
Assert-Equal $stationBlueprint.salable $false 'animal care station blueprint cannot be sold back'
Assert-Equal $stationBlueprint.buying_price 1000 'animal care station blueprint purchase price'
Assert-Equal $stationBlueprint.overlay 1 'animal care station blueprint stack size'
Assert-Equal $stationBlueprint.function.'$type' 'ItemFunctionRecipe' 'animal care station blueprint native function'
Assert-Equal $stationBlueprint.function.recipe_id 'animal_care_station' 'animal care station blueprint recipe reference'
Assert-Equal $stationBlueprint.ui_sprite_asset.url 'sprite_equipment_animal_care_station' 'animal care station blueprint icon'

$stationItem = $items['animal_care_station']
Assert-Equal $stationItem.function.'$type' 'ItemFunctionEquipment' 'animal care station item native function'
Assert-Equal $stationItem.salable $true 'animal care station can be sold'
Assert-Equal $stationItem.selling_price -1 'animal care station uses native recipe-derived selling price sentinel'
Assert-Equal $stationItem.buying_price 0 'animal care station is not directly shop-priced'
Assert-Equal $stationItem.electric_energy 0 'placed-equipment item does not masquerade as a carried battery'

$directPrices = @{
    hatch_egg = 175
    petrified_hatch_egg = 2300
    drecko_egg = 1000
    mole_egg = 400
    fertile_mole_egg = 1200
    oilfloater_egg = 235
    polymer_oilfloater_egg = 4000
}
$expectedItemIcons = @{
    hatch_egg = 'icon_item_hatch_egg'
    petrified_hatch_egg = 'icon_item_petrified_hatch_egg'
    drecko_egg = 'icon_item_drecko_egg'
    mole_egg = 'icon_item_mole_egg'
    fertile_mole_egg = 'icon_item_fertile_mole_egg'
    oilfloater_egg = 'icon_item_oilfloater_egg'
    polymer_oilfloater_egg = 'icon_item_polymer_oilfloater_egg'
}
$expectedIconSources = @{
    hatch_egg = 'hatch/rendered_png/egg_hatch_anim/ui/ui_000.png'
    petrified_hatch_egg = 'hatch/rendered_png/egg_hatch_anim/mtl_ui/mtl_ui_000.png'
    drecko_egg = 'drecko/rendered_png/egg_drecko_anim/ui/ui_000.png'
    mole_egg = 'mole/rendered_png/egg_driller_anim/ui/ui_000.png'
    fertile_mole_egg = 'mole/rendered_png/egg_driller_anim/del_ui/del_ui_000.png'
    oilfloater_egg = 'oilfloater/rendered_png/egg_oilfloater_anim/ui/ui_000.png'
    polymer_oilfloater_egg = 'oilfloater/rendered_png/egg_oilfloater_anim/hot_ui/hot_ui_000.png'
}
Assert-ExactSet @($expectedItemIcons.Keys) @($directPrices.Keys) 'egg icon item set'
foreach ($itemId in @($directPrices.Keys)) {
    Assert-Equal $items[$itemId].selling_price $directPrices[$itemId] "$itemId direct selling price"
    Assert-Equal $items[$itemId].sub_type 'husbandry_animal_product' "$itemId subtype"
    Assert-Equal $items[$itemId].ui_sprite_asset.url $expectedItemIcons[$itemId] "$itemId dedicated egg icon"
}

$itemIconMap = @{}
foreach ($itemIcon in @($assetConfig.itemIcons.files)) {
    $itemId = [string]$itemIcon.itemId
    Assert-True (-not [string]::IsNullOrWhiteSpace($itemId)) 'item icon config contains an empty itemId'
    Assert-True (-not $itemIconMap.ContainsKey($itemId)) "item icon config contains duplicate itemId '$itemId'"
    $itemIconMap[$itemId] = $itemIcon
}
Assert-ExactSet @($itemIconMap.Keys) @($expectedItemIcons.Keys) 'item icon config species-product set'
$iconNormalization = $assetConfig.itemIcons.normalization
Assert-Equal $iconNormalization.canvasWidth 28 'egg icon normalized canvas width'
Assert-Equal $iconNormalization.canvasHeight 28 'egg icon normalized canvas height'
Assert-Equal $iconNormalization.contentWidth 24 'egg icon normalized content width'
Assert-Equal $iconNormalization.contentHeight 24 'egg icon normalized content height'
Assert-Equal $iconNormalization.alignment 'center' 'egg icon normalized alignment'
Assert-Equal $iconNormalization.resampling 'high-quality-bicubic' 'egg icon normalized resampling'
Assert-Equal $iconNormalization.pixelsPerUnit 8 'egg icon default PPU contract'
foreach ($itemId in @($expectedItemIcons.Keys)) {
    $itemIcon = $itemIconMap[$itemId]
    Assert-Equal $itemIcon.source $expectedIconSources[$itemId] "$itemId selected ONI egg variant"
    Assert-Equal $itemIcon.destination "Content/Sprites/$($expectedItemIcons[$itemId]).png" "$itemId assembled icon destination"
    Assert-True ([string]$itemIcon.sha256 -cmatch '^[0-9A-F]{64}$') "$itemId source SHA-256 format"
    Assert-True ([string]$itemIcon.outputSha256 -cmatch '^[0-9A-F]{64}$') "$itemId normalized-output SHA-256 format"
}

$nativePrices = @{
    iron_ore = 40
    copper_ore = 12
    gold_ore = 300
    titanium_ore = 100
    wool = 500
    wool_grease = 1000
    meat = 60
    organic_fertilizer = 50
    coal = 10
    resin = 30
    rubber = 50
    plastic = 150
}
$nativeItemTablePath = Join-Path $repoRoot 'references\doloc-town\reverse\builds\24966367_public_958EAF\asset-ripper-unity-project\ExportedProject\Assets\Configs\GenDatas\item_tbitem.json'
$nativePriceAuthority = 'frozen 1.00.06 expectations'
if (Test-Path -LiteralPath $nativeItemTablePath -PathType Leaf) {
    $nativeItems = New-IdMap (Read-Json $nativeItemTablePath) '1.00.06 native item table'
    foreach ($itemId in @($nativePrices.Keys)) {
        Assert-True $nativeItems.ContainsKey($itemId) "1.00.06 native item '$itemId' exists"
        Assert-Equal $nativeItems[$itemId].selling_price $nativePrices[$itemId] "1.00.06 native price for $itemId"
    }
    $nativePriceAuthority = 'verified against local 24966367 / 1.00.06 table'
}

$recipeSpecs = @(
    [pscustomobject]@{ Recipe = 'animal_care_hatch_egg'; Input = 'hatch_egg'; Lut = 'animal_care_hatch_egg_drop'; Outputs = @{ iron_ore = 5; copper_ore = 5 }; Value = 260 },
    [pscustomobject]@{ Recipe = 'animal_care_petrified_hatch_egg'; Input = 'petrified_hatch_egg'; Lut = 'animal_care_petrified_hatch_egg_drop'; Outputs = @{ gold_ore = 10; titanium_ore = 5 }; Value = 3500 },
    [pscustomobject]@{ Recipe = 'animal_care_drecko_egg'; Input = 'drecko_egg'; Lut = 'animal_care_drecko_egg_drop'; Outputs = @{ wool = 3 }; Value = 1500 },
    [pscustomobject]@{ Recipe = 'animal_care_mole_egg'; Input = 'mole_egg'; Lut = 'animal_care_mole_egg_drop'; Outputs = @{ meat = 10 }; Value = 600 },
    [pscustomobject]@{ Recipe = 'animal_care_fertile_mole_egg'; Input = 'fertile_mole_egg'; Lut = 'animal_care_fertile_mole_egg_drop'; Outputs = @{ meat = 5; organic_fertilizer = 30 }; Value = 1800 },
    [pscustomobject]@{ Recipe = 'animal_care_oilfloater_egg'; Input = 'oilfloater_egg'; Lut = 'animal_care_oilfloater_egg_drop'; Outputs = @{ coal = 20; resin = 5 }; Value = 350 },
    [pscustomobject]@{ Recipe = 'animal_care_polymer_oilfloater_egg'; Input = 'polymer_oilfloater_egg'; Lut = 'animal_care_polymer_oilfloater_egg_drop'; Outputs = @{ rubber = 30; plastic = 30 }; Value = 6000 }
)

$economyRows = New-Object 'System.Collections.Generic.List[object]'
foreach ($spec in $recipeSpecs) {
    Assert-True $dismantleRecipes.ContainsKey($spec.Recipe) "dismantle recipe '$($spec.Recipe)' exists"
    $recipe = $dismantleRecipes[$spec.Recipe]
    Assert-Equal $recipe.cost_time 12 "$($spec.Recipe) one-hour cost in native TU"
    Assert-Equal @($recipe.input_items).Count 1 "$($spec.Recipe) input row count"
    Assert-Equal $recipe.input_items[0].item_name $spec.Input "$($spec.Recipe) input item"
    Assert-Equal $recipe.input_items[0].item_count 1 "$($spec.Recipe) input count"
    Assert-Equal $recipe.output_item_spawn_entry.spawn_lut $spec.Lut "$($spec.Recipe) output LUT"
    Assert-True $spawnLuts.ContainsKey($spec.Lut) "output LUT '$($spec.Lut)' exists"

    $lutRows = @($spawnLuts[$spec.Lut].spawn_datas)
    $actualOutputs = @{}
    $totalCount = 0
    $totalValue = 0
    foreach ($row in $lutRows) {
        $outputId = [string]$row.item_name
        Assert-Equal $row.min_count $row.max_count "$($spec.Lut) deterministic count for $outputId"
        $count = [int]$row.max_count
        $actualOutputs[$outputId] = $count
        $totalCount += $count
        Assert-True $nativePrices.ContainsKey($outputId) "known 1.00.06 price for $outputId"
        $totalValue += $count * [int]$nativePrices[$outputId]
    }
    Assert-ExactSet @($actualOutputs.Keys) @($spec.Outputs.Keys) "$($spec.Recipe) output item set"
    foreach ($outputId in @($spec.Outputs.Keys)) {
        Assert-Equal $actualOutputs[$outputId] $spec.Outputs[$outputId] "$($spec.Recipe) output count for $outputId"
    }
    Assert-Equal $recipe.output_item_spawn_entry.count_range.min_count $totalCount "$($spec.Recipe) total minimum draw count"
    Assert-Equal $recipe.output_item_spawn_entry.count_range.max_count $totalCount "$($spec.Recipe) total maximum draw count"
    Assert-Equal $totalValue $spec.Value "$($spec.Recipe) processed value"

    $directPrice = [int]$directPrices[$spec.Input]
    $upliftPercent = [Math]::Round((($totalValue - $directPrice) / [double]$directPrice) * 100, 1)
    $economyRows.Add([pscustomobject]@{
        Input = $spec.Input
        Direct = $directPrice
        Processed = $totalValue
        UpliftPercent = $upliftPercent
    }) | Out-Null
}

Assert-True $dismantleGroups.ContainsKey('animal_care_station') 'animal care dismantle group exists'
$group = $dismantleGroups['animal_care_station']
Assert-Equal $group.time_ratio 1 'animal care station processing time ratio'
Assert-Equal $group.max_craft_count 5 'animal care station max craft count'
Assert-ExactSet @($group.recipe_ids) @($recipeSpecs.Recipe) 'animal care station recipe set'
Assert-True $equipment.ContainsKey('animal_care_station') 'animal care station equipment row exists'
Assert-Equal $equipment['animal_care_station'].function.'$type' 'EquipmentFuncGarbageShredder' 'animal care station native function'
Assert-Equal $equipment['animal_care_station'].function.recipe_group_name 'animal_care_station' 'animal care station recipe group reference'
Assert-Equal $equipment['animal_care_station'].function.interval 12 'animal care station native work interval'
Assert-Equal $equipment['animal_care_station'].electronic_component.'$type' 'EComProtoAppliance' 'animal care station electrical component type'
Assert-Equal $equipment['animal_care_station'].electronic_component.threshold 1 'animal care station rated power consumption'
$stationNormalization = $assetConfig.careStation.normalization
Assert-Equal $equipment['animal_care_station'].cover_size.x $stationNormalization.coverSize.x 'animal care station cover width'
Assert-Equal $equipment['animal_care_station'].cover_size.y $stationNormalization.coverSize.y 'animal care station cover height'
Assert-Equal $stationNormalization.coverSize.x 2 'animal care station frozen two-tile cover width'
Assert-Equal $stationNormalization.coverSize.y 4 'animal care station frozen four-tile cover height'
Assert-True $buildRecipes.ContainsKey('animal_care_station') 'animal care station build recipe exists'
$stationBuildRecipe = $buildRecipes['animal_care_station']
Assert-Equal $stationBuildRecipe.default_unlock $false 'animal care station requires its blueprint'
$stationBuildInputs = @{}
foreach ($inputRow in @($stationBuildRecipe.input_items)) {
    Assert-True (-not $stationBuildInputs.ContainsKey([string]$inputRow.item_name)) "animal care station build recipe duplicate input '$($inputRow.item_name)'"
    $stationBuildInputs[[string]$inputRow.item_name] = [int]$inputRow.item_count
}
Assert-ExactSet @($stationBuildInputs.Keys) @('copper_ingot', 'wood') 'animal care station build input set'
Assert-Equal $stationBuildInputs['copper_ingot'] 5 'animal care station copper-ingot cost'
Assert-Equal $stationBuildInputs['wood'] 100 'animal care station wood cost'
if (Test-Path -LiteralPath $nativeItemTablePath -PathType Leaf) {
    Assert-True $nativeItems.ContainsKey('copper_ingot') '1.00.06 native copper_ingot exists'
    Assert-True $nativeItems.ContainsKey('wood') '1.00.06 native wood exists'
    Assert-Equal $nativeItems['copper_ingot'].selling_price 50 '1.00.06 native copper-ingot selling price'
    Assert-Equal $nativeItems['wood'].selling_price 4 '1.00.06 native wood selling price'
    $stationDerivedSellingPrice =
        ([int]$nativeItems['copper_ingot'].selling_price * [int]$stationBuildInputs['copper_ingot']) +
        ([int]$nativeItems['wood'].selling_price * [int]$stationBuildInputs['wood'])
    Assert-Equal $stationDerivedSellingPrice 650 'animal care station native recipe-derived selling price'
}

$nativeGlobalParameterPath = Join-Path $repoRoot 'references\doloc-town\reverse\builds\24966367_public_958EAF\asset-ripper-unity-project\ExportedProject\Assets\Configs\GenDatas\settings_tbglobalparameter.json'
if (Test-Path -LiteralPath $nativeGlobalParameterPath -PathType Leaf) {
    $nativeGlobalParameterRows = @(Read-Json $nativeGlobalParameterPath)
    Assert-Equal $nativeGlobalParameterRows.Count 1 '1.00.06 native global-parameter row count'
    Assert-Equal $nativeGlobalParameterRows[0].TU2Min 5 '1.00.06 native minutes per TU'
    Assert-Equal $nativeGlobalParameterRows[0].Hour2Min 60 '1.00.06 native minutes per hour'
    Assert-Equal (12 * [int]$nativeGlobalParameterRows[0].TU2Min) ([int]$nativeGlobalParameterRows[0].Hour2Min) 'animal care station 12 TU equals one game hour'
}

$extensions = @(Read-Json (Join-Path $contentRoot 'mod_tbmodrecipegroupextension.json'))
$workbench = @($extensions | Where-Object { $_.id -eq 'equipment_workbench' })
Assert-Equal $workbench.Count 1 'equipment workbench extension count'
Assert-ExactSet @($workbench[0].extra_recipes) @('animal_care_station') 'equipment workbench added recipe'

$storeExtensions = @(Read-Json (Join-Path $contentRoot 'mod_tbmodstoreextension.json'))
$animalShop = @($storeExtensions | Where-Object { $_.id -eq 'animal_shop' })
Assert-Equal $animalShop.Count 1 'animal shop extension count'
Assert-ExactSet @($animalShop[0].extra_items.item_name) @('sack_hatch', 'sack_drecko', 'sack_mole', 'sack_oilfloater', 'recipe_animal_care_station') 'animal shop fixed item set'
$storeBags = @($animalShop[0].extra_items | Where-Object { $_.item_name -like 'sack_*' })
Assert-Equal $storeBags.Count 4 'animal shop bag count'
foreach ($storeBag in $storeBags) {
    Assert-Equal $storeBag.storage 0 "$($storeBag.item_name) animal-shop storage mode"
    Assert-Equal $storeBag.default_unlock $true "$($storeBag.item_name) animal-shop default unlock"
    Assert-Equal @($storeBag.season_spawn_data).Count 4 "$($storeBag.item_name) season stock rows"
    foreach ($seasonIndex in 0..3) {
        $seasonRow = $storeBag.season_spawn_data[$seasonIndex]
        Assert-Equal $seasonRow.spawn_weight 0 "$($storeBag.item_name) season $seasonIndex fixed-shelf weight"
        if ($seasonIndex -lt 3) {
            Assert-Equal $seasonRow.count_range.min_count 20 "$($storeBag.item_name) season $seasonIndex minimum stock"
            Assert-Equal $seasonRow.count_range.max_count 20 "$($storeBag.item_name) season $seasonIndex maximum stock"
        }
        else {
            Assert-Equal $seasonRow.count_range.min_count 1 "$($storeBag.item_name) season $seasonIndex minimum stock"
            Assert-Equal $seasonRow.count_range.max_count 3 "$($storeBag.item_name) season $seasonIndex maximum stock"
        }
    }
}
$storeBlueprint = @($animalShop[0].extra_items | Where-Object { $_.item_name -eq 'recipe_animal_care_station' })
Assert-Equal $storeBlueprint.Count 1 'animal shop station-blueprint count'
Assert-Equal $storeBlueprint[0].storage 1 'animal shop station-blueprint storage mode'
Assert-Equal $storeBlueprint[0].default_unlock $true 'animal shop station-blueprint shelf unlock'
Assert-Equal @($storeBlueprint[0].season_spawn_data).Count 4 'animal shop station-blueprint season stock rows'
foreach ($seasonIndex in 0..3) {
    $seasonRow = $storeBlueprint[0].season_spawn_data[$seasonIndex]
    Assert-Equal $seasonRow.spawn_weight 0 "station blueprint season $seasonIndex fixed-shelf weight"
    Assert-Equal $seasonRow.count_range.min_count 1 "station blueprint season $seasonIndex minimum stock"
    Assert-Equal $seasonRow.count_range.max_count 1 "station blueprint season $seasonIndex maximum stock"
}

$stationMeta = Read-Json (Join-Path $contentRoot 'Sprites\sprite_equipment_animal_care_station.json')
Assert-Equal $assetConfig.careStation.sha256 '28A709F619A84BBD311B5DB7F5DBC0C93F8AE0D2E8F078B63397D3EF4389D296' 'care station source SHA-256'
Assert-True ([string]$assetConfig.careStation.outputSha256 -cmatch '^[0-9A-F]{64}$') 'care station normalized-output SHA-256 format'
Assert-Equal $stationNormalization.canvasWidth 32 'care station normalized canvas width'
Assert-Equal $stationNormalization.canvasHeight 48 'care station normalized canvas height'
Assert-Equal $stationNormalization.contentWidth 32 'care station normalized content width'
Assert-Equal $stationNormalization.contentHeight 46 'care station normalized content height'
Assert-Equal $stationNormalization.alignment 'bottom-center' 'care station normalized alignment'
Assert-Equal $stationNormalization.bottomPadding 1 'care station normalized bottom padding'
Assert-Equal $stationNormalization.resampling 'high-quality-bicubic' 'care station normalized resampling'
Assert-Equal $stationMeta.pivot.x $stationNormalization.pivot.x 'care station sprite pivot x'
Assert-Equal $stationMeta.pivot.y $stationNormalization.pivot.y 'care station sprite pivot y'
Assert-Equal $stationMeta.pixels_per_unit $stationNormalization.pixelsPerUnit 'care station sprite PPU'
Assert-Equal $stationMeta.pivot.x 16 'care station frozen bottom-center pivot x'
Assert-Equal $stationMeta.pivot.y 1 'care station frozen ground pivot y'
Assert-Equal $stationMeta.pixels_per_unit 8 'care station native PPU'

if ($isPackage) {
    Assert-True (Test-Path -LiteralPath (Join-Path $dataRoot 'info.json') -PathType Leaf) 'assembled package info.json exists'
    $stationPngPath = Join-Path $contentRoot 'Sprites\sprite_equipment_animal_care_station.png'
    Assert-True (Test-Path -LiteralPath $stationPngPath -PathType Leaf) 'assembled station PNG exists'
    $stationGeometry = Get-PngGeometry $stationPngPath
    Assert-Equal $stationGeometry.CanvasWidth $stationNormalization.canvasWidth 'assembled station PNG canvas width'
    Assert-Equal $stationGeometry.CanvasHeight $stationNormalization.canvasHeight 'assembled station PNG canvas height'
    Assert-True ($stationGeometry.VisibleWidth -le [int]$stationNormalization.contentWidth) 'assembled station visible width stays inside normalized bound'
    Assert-True ($stationGeometry.VisibleHeight -le [int]$stationNormalization.contentHeight) 'assembled station visible height stays inside normalized bound'
    Assert-Equal $stationGeometry.BottomPadding $stationNormalization.bottomPadding 'assembled station visible bottom is ground-aligned'
    Assert-True ([Math]::Abs($stationGeometry.LeftPadding - $stationGeometry.RightPadding) -le 1) 'assembled station is horizontally centered'
    Assert-Equal (Get-FileHash -LiteralPath $stationPngPath -Algorithm SHA256).Hash ([string]$assetConfig.careStation.outputSha256) 'assembled station normalized-output hash'
    foreach ($itemId in @($expectedItemIcons.Keys)) {
        $itemIcon = $itemIconMap[$itemId]
        $iconPath = Join-Path $dataRoot (([string]$itemIcon.destination).Replace('/', [IO.Path]::DirectorySeparatorChar))
        Assert-True (Test-Path -LiteralPath $iconPath -PathType Leaf) "assembled egg icon exists: $itemId"
        $iconGeometry = Get-PngGeometry $iconPath
        Assert-Equal $iconGeometry.CanvasWidth $iconNormalization.canvasWidth "$itemId normalized icon canvas width"
        Assert-Equal $iconGeometry.CanvasHeight $iconNormalization.canvasHeight "$itemId normalized icon canvas height"
        Assert-True ($iconGeometry.VisibleWidth -le [int]$iconNormalization.contentWidth) "$itemId visible icon width stays inside normalized bound"
        Assert-True ($iconGeometry.VisibleHeight -le [int]$iconNormalization.contentHeight) "$itemId visible icon height stays inside normalized bound"
        Assert-True ([Math]::Abs($iconGeometry.LeftPadding - $iconGeometry.RightPadding) -le 1) "$itemId icon is horizontally centered"
        Assert-True ([Math]::Abs($iconGeometry.TopPadding - $iconGeometry.BottomPadding) -le 1) "$itemId icon is vertically centered"
        Assert-Equal (Get-FileHash -LiteralPath $iconPath -Algorithm SHA256).Hash ([string]$itemIcon.outputSha256) "assembled egg icon normalized-output hash: $itemId"
    }
    foreach ($animal in @($assetConfig.animals)) {
        $frameManifestPath = Join-Path $contentRoot ("Sprites\{0}" -f [string]$animal.frameManifest)
        $frameManifest = Read-Json $frameManifestPath
        $expectedFrames = @($frameManifest.states | ForEach-Object { @($_.files) } | Select-Object -Unique)
        foreach ($frameName in $expectedFrames) {
            Assert-True (Test-Path -LiteralPath (Join-Path $contentRoot ("Sprites\{0}" -f [string]$frameName)) -PathType Leaf) "assembled frame exists: $frameName"
        }
        foreach ($audioName in @($animal.audioFiles)) {
            Assert-True (Test-Path -LiteralPath (Join-Path $contentRoot ("Audio\{0}" -f [string]$audioName)) -PathType Leaf) "assembled WAV exists: $audioName"
        }
    }
}

$ordinaryDaily = @(
    [pscustomobject]@{ Species = 'hatch'; Days = 1; ProcessedPerCycle = 260; Space = 2 },
    [pscustomobject]@{ Species = 'drecko'; Days = 3; ProcessedPerCycle = 1500; Space = 3 },
    [pscustomobject]@{ Species = 'mole'; Days = 1; ProcessedPerCycle = 600; Space = 4 },
    [pscustomobject]@{ Species = 'oilfloater'; Days = 1; ProcessedPerCycle = 350; Space = 2 }
) | ForEach-Object {
    [pscustomobject]@{
        Species = $_.Species
        Daily = [Math]::Round($_.ProcessedPerCycle / [double]$_.Days, 2)
        PerSpaceDaily = [Math]::Round(($_.ProcessedPerCycle / [double]$_.Days) / $_.Space, 2)
    }
}

$hiddenValuePerThreshold = @(
    [pscustomobject]@{ Species = 'hatch'; Value = 3500; Threshold = 60 },
    [pscustomobject]@{ Species = 'drecko'; Value = 3000; Threshold = 60 },
    [pscustomobject]@{ Species = 'mole'; Value = 1800; Threshold = 40 },
    [pscustomobject]@{ Species = 'oilfloater'; Value = 6000; Threshold = 120 }
) | ForEach-Object {
    [pscustomobject]@{
        Species = $_.Species
        ValuePerThreshold = [Math]::Round($_.Value / [double]$_.Threshold, 2)
    }
}

Write-Output "AnimalPack static validation PASS ($nativePriceAuthority)."
Write-Output 'Direct-sale versus care-station values:'
$economyRows | Format-Table -AutoSize | Out-String | Write-Output
Write-Output 'Ordinary processed value per day and per animal-space:'
$ordinaryDaily | Format-Table -AutoSize | Out-String | Write-Output
Write-Output 'Hidden value per contribution threshold point:'
$hiddenValuePerThreshold | Format-Table -AutoSize | Out-String | Write-Output
