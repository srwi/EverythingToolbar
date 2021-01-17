<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:msxsl="urn:schemas-microsoft-com:xslt"
                exclude-result-prefixes="msxsl"
                xmlns:wix="http://schemas.microsoft.com/wix/2006/wi"
                xmlns:my="my:my">

	<xsl:output method="xml" indent="yes" />

	<xsl:strip-space elements="*"/>

	<xsl:template match="@*|node()">
		<xsl:copy>
			<xsl:apply-templates select="@*|node()"/>
		</xsl:copy>
	</xsl:template>

	<!-- Exclude EverythingToolbar.dll from Heat generated file so we can extract the AssemblyVersion from it specifically -->
	<xsl:key name="everythingToolbarDll" match="wix:Component[wix:File[@Source = '$(var.HarvestPath)\EverythingToolbar.dll']]" use="@Id"/>
	<xsl:template match="*[self::wix:Component or self::wix:ComponentRef][key('everythingToolbarDll', @Id)]" />

	<xsl:key name="DllConfigFile" match="wix:Component[wix:File[@Source = '$(var.HarvestPath)\EverythingToolbar.dll.config']]" use="@Id"/>
	<xsl:template match="*[self::wix:Component or self::wix:ComponentRef][key('DllConfigFile', @Id)]" />
</xsl:stylesheet>