<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:wix="http://schemas.microsoft.com/wix/2006/wi">

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
	<!-- Exclude EverythingToolbar.Launcher.exe from Heat generated file so we can use it for launch after installation -->
	<xsl:key name="everythingToolbarLauncherExe" match="wix:Component[wix:File[@Source = '$(var.HarvestPath)\EverythingToolbar.Launcher.exe']]" use="@Id"/>
	<xsl:template match="*[self::wix:Component or self::wix:ComponentRef][key('everythingToolbarLauncherExe', @Id)]" />

	<xsl:key name="DllConfigFile" match="wix:Component[wix:File[@Source = '$(var.HarvestPath)\EverythingToolbar.dll.config']]" use="@Id"/>
	<xsl:template match="*[self::wix:Component or self::wix:ComponentRef][key('DllConfigFile', @Id)]" />
	<xsl:key name="ExeConfigFile" match="wix:Component[wix:File[@Source = '$(var.HarvestPath)\EverythingToolbar.Launcher.exe.config']]" use="@Id"/>
	<xsl:template match="*[self::wix:Component or self::wix:ComponentRef][key('ExeConfigFile', @Id)]" />
</xsl:stylesheet>