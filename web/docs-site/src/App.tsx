import { RedocStandalone } from 'redoc'

export default function App() {
  return (
    <div className="min-h-screen">
      <RedocStandalone
        specUrl="/openapi.yaml"
        options={{
          theme: {
            colors: {
              primary: {
                main: '#d14524',
              },
            },
            typography: {
              fontSize: '15px',
              fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif',
              headings: {
                fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif',
              },
            },
          },
          hideDownloadButton: false,
          hideHostname: false,
          expandResponses: '200,201',
          jsonSampleExpandLevel: 2,
          scrollYOffset: 0,
          hideLoading: false,
          nativeScrollbars: false,
          pathInMiddlePanel: false,
          requiredPropsFirst: true,
          sortPropsAlphabetically: false,
          showExtensions: false,
          hideSingleRequestSampleTab: true,
        }}
      />
    </div>
  )
}
