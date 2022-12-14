<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:template match="/">
        <xsl:variable name="var1_initial" select="."/>
        <TransformedRouteAndBOM_DataSet>
            <xsl:for-each select="NewDataSet">
                <xsl:variable name="var2_cur" select="."/>
                <xsl:for-each select="BillOfMaterials[MaterialUse='Consume']">
                    <xsl:variable name="var3_cur" select="."/>
                    <TransformedRouteAndBOM>
                        <xsl:variable name="RoutingKey">
                            <xsl:value-of select="RoutingKey"/>
                        </xsl:variable>
                        <RoutingKey>
                            <xsl:value-of select="$RoutingKey"/>
                        </RoutingKey>                       
                        <Sequence>
                            <xsl:value-of select="$var2_cur/Routings[RoutingKey=$RoutingKey]/Sequence"/>
                        </Sequence>
                        <RouteNumber>
                            <xsl:value-of select="number($var2_cur/Routings[RoutingKey=$RoutingKey]/RouteNumber)"/>
                        </RouteNumber>
                        <ComponentMaterial>
                            <xsl:value-of select="ComponentMaterial"/>
                        </ComponentMaterial>
                        <RequiredQuantity>
                            <xsl:value-of select="number(RequiredQuantity)"/>
                        </RequiredQuantity>
                        <MaterialUse>
                            <xsl:value-of select="MaterialUse"/>
                        </MaterialUse>
                    </TransformedRouteAndBOM>
                </xsl:for-each>
            </xsl:for-each>
        </TransformedRouteAndBOM_DataSet>
    </xsl:template>
</xsl:stylesheet>