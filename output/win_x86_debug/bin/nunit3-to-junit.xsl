<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
	<xsl:output method="xml" indent="yes" />

	<xsl:template name="filename">
		<xsl:param name="str" select="."/>
		<xsl:choose>
			<xsl:when test="contains($str, '/')">
				<xsl:call-template name="filename">
					<xsl:with-param name="str" select="substring-after($str, '/')" />
				</xsl:call-template>
			</xsl:when>
			<xsl:when test="contains($str, '\')">
				<xsl:call-template name="filename">
					<xsl:with-param name="str" select="substring-after($str, '\')" />
				</xsl:call-template>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="$str" />
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template name="test-suite">
		<xsl:for-each select="test-case">
			<testcase classname="{../@fullname}" name="{@name}" time="{@duration}">
				<xsl:if test="./failure">
					<failure message="{./failure/message}">
						<xsl:value-of select="./failure/stack-trace" />
					</failure>
				</xsl:if>
				<xsl:if test="@result='Skipped'">
					<skipped message="{./reason/message}"/>
				</xsl:if>
			</testcase>
		</xsl:for-each>
		<xsl:for-each select="test-suite">
			<xsl:call-template name="test-suite" />
		</xsl:for-each>
	</xsl:template>
	
	<xsl:template match="/test-run">
		<testsuites>
			<xsl:for-each select="test-suite[@type='Assembly']">
				<testsuite tests="{@total}" time="{@duration}" failures="{@failed}" errors="0" skipped="{@skipped}">
					<xsl:attribute name="name">
						<xsl:call-template name="filename">
							<xsl:with-param name="str" select="@name" />
						</xsl:call-template>
					</xsl:attribute>
					<xsl:for-each select="test-suite">
						<xsl:call-template name="test-suite" />
					</xsl:for-each>
				</testsuite>
			</xsl:for-each>
		</testsuites>
	</xsl:template>
</xsl:stylesheet>
