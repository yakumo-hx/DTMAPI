if ($smokeSaveEnvironmentScopeApplied) {
        try {
            [Environment]::SetEnvironmentVariable(
                'DTMAPI_DOLOC_PERSISTENT_ROOT',
                $previousDtmApiPersistentRoot,
                [EnvironmentVariableTarget]::Process)
        }
        finally {
            [Environment]::SetEnvironmentVariable(
                'DTMAPI_STATE_DIR',
                $previousDtmApiStateDir,
                [EnvironmentVariableTarget]::Process)
        }
    }
