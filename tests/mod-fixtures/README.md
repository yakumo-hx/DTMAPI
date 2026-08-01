# Mod fixtures

- `qa/` contains explicit-install test Mods and synthetic admission fixtures.
- `negative/` contains intentionally invalid inputs used to prove fail-closed behavior.

Neither subtree is part of the player Runtime or the ordinary Workshop product set. Release and
install tooling must require the fixture's explicit Catalog lane instead of inferring eligibility
from a project file or manifest.
