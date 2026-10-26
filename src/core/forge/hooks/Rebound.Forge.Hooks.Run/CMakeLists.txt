cmake_minimum_required(VERSION 3.24)

project(ReboundForgeHooksRun LANGUAGES CXX)

if(NOT REBOUND_TARGET_ARCH STREQUAL "x64"
   AND NOT REBOUND_TARGET_ARCH STREQUAL "ARM64")
    message(FATAL_ERROR
        "Set REBOUND_TARGET_ARCH to x64 or ARM64."
    )
endif()

if(REBOUND_TARGET_ARCH STREQUAL "ARM64")
    set(MINHOOK_DETOURS_LIB
        "${CMAKE_CURRENT_SOURCE_DIR}/lib/minhook-detours.ARM64.Release.lib")
else()
    set(MINHOOK_DETOURS_LIB
        "${CMAKE_CURRENT_SOURCE_DIR}/lib/minhook-detours.x64.Release.lib")
endif()

if(NOT EXISTS "${MINHOOK_DETOURS_LIB}")
    message(FATAL_ERROR "Library not found: ${MINHOOK_DETOURS_LIB}")
endif()

set(COMMON_SOURCES
    "${CMAKE_CURRENT_SOURCE_DIR}/src/dllmain.cpp"
    "${CMAKE_CURRENT_SOURCE_DIR}/src/pch.cpp"
)

add_library(ReboundForgeHooksRun SHARED ${COMMON_SOURCES})

target_compile_features(ReboundForgeHooksRun PRIVATE cxx_std_20)

target_compile_definitions(ReboundForgeHooksRun PRIVATE
    UNICODE
    _UNICODE
    REBOUNDFORGEHOOKSRUN_EXPORTS
    _WINDOWS
    _USRDLL
    $<$<CONFIG:Debug>:_DEBUG>
    $<$<CONFIG:Release>:NDEBUG>
)

target_precompile_headers(ReboundForgeHooksRun PRIVATE
    "${CMAKE_CURRENT_SOURCE_DIR}/src/pch.h"
)

target_include_directories(ReboundForgeHooksRun PRIVATE
    "${CMAKE_CURRENT_SOURCE_DIR}/src"
    "${CMAKE_CURRENT_SOURCE_DIR}/lib"
)

target_link_libraries(ReboundForgeHooksRun PRIVATE
    "${MINHOOK_DETOURS_LIB}"
)

set_target_properties(ReboundForgeHooksRun PROPERTIES
    OUTPUT_NAME "Rebound.Forge.Hooks.Run.${REBOUND_TARGET_ARCH}"
    PREFIX ""
    RUNTIME_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/bin"
    LIBRARY_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/bin"
)