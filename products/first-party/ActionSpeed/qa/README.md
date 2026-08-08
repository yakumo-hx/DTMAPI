# ActionSpeed QA seam

The player DLL exposes no public QA contract. Focused QA observes the loaded managed product and its private `ActionSpeedNativeRuntime` by external reflection, then records Hook count, animator snapshot count, pending animal marker, auto-fill state, and lifecycle restoration.
