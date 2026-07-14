package com.umbral.app;

import android.content.Intent;
import android.graphics.Color;
import android.net.Uri;
import android.os.Bundle;
import com.getcapacitor.BridgeActivity;

public class MainActivity extends BridgeActivity {

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        // Required by @capacitor-community/barcode-scanner: Activity window must
        // support transparency so the native camera layer shows through the WebView.
        getWindow().setBackgroundDrawableResource(android.R.color.transparent);
    }

    @Override
    protected void onNewIntent(Intent intent) {
        super.onNewIntent(intent);
        setIntent(intent);
        handleDeepLink(intent);
    }

    private void handleDeepLink(Intent intent) {
        Uri data = intent.getData();
        if (data == null) return;
        if (!"umbral".equals(data.getScheme()) || !"callback".equals(data.getHost())) return;

        String query = data.getQuery();
        String callbackUrl = "http://localhost/callback" + (query != null ? "?" + query : "");

        runOnUiThread(() -> bridge.getWebView().loadUrl(callbackUrl));
    }
}
